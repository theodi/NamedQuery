using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TheODI.NamedQuery.Validation;

[assembly: HostingStartup(typeof(Startup))]

namespace TheODI.NamedQuery.Validation;

internal sealed class Startup : IHostingStartup
{
    void IHostingStartup.Configure(IWebHostBuilder builder) => builder
        .ConfigureLogging(logging => logging.ClearProviders()) // MSBuild's Exec parses this process's stdout for errors, so the app's own logs must not go there: a log line that looks like a compiler error would fail the build, and the rest would clutter the build log.
        .ConfigureServices(services => services.AddSingleton<IStartupFilter, ValidationFilter>());

    private sealed class ValidationFilter : IStartupFilter
    {
        Action<IApplicationBuilder> IStartupFilter.Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);

            Validate(app.ApplicationServices);
        };

        private static void Validate(IServiceProvider services)
        {
            if (services.GetService<Resources>() is not null)
            {
                foreach (var diagnostic in Validator.Validate(services))
                {
                    Console.Out.WriteLine(diagnostic);
                }
            }

            Environment.Exit(0);
        }
    }
}
