using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TheODI.NamedQuery.Validation;

[assembly: HostingStartup(typeof(Startup))]

namespace TheODI.NamedQuery.Validation;

internal sealed class Startup : IHostingStartup
{
    private static int completed;

    private static string AssemblyPath => Assembly.GetEntryAssembly()?.Location ?? string.Empty;

    void IHostingStartup.Configure(IWebHostBuilder builder)
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            if (Interlocked.Exchange(ref completed, 1) == 0)
            {
                HostError("The application exited before the host started.");
            }
        };

        builder
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices(services => services.AddSingleton<IStartupFilter, ValidationFilter>());
    }

    private static void HostError(string message) => Console.Out.WriteLine(new Diagnostic(DiagnosticCodes.HostFailure, AssemblyPath, message));

    private sealed class ValidationFilter : IStartupFilter
    {
        Action<IApplicationBuilder> IStartupFilter.Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);

            Validate(app.ApplicationServices);
        };

        private static void Validate(IServiceProvider services)
        {
            Interlocked.Exchange(ref completed, 1);

            try
            {
                if (services.GetService<Resources>() is not null)
                {
                    foreach (var diagnostic in Validator.Validate(services))
                    {
                        Console.Out.WriteLine(diagnostic);
                    }
                }
            }
            catch (Exception e)
            {
                HostError($"Validation failed: {e.Message.ReplaceLineEndings(" ")}");
            }

            Console.Out.Flush();
            Environment.Exit(0);
        }
    }
}
