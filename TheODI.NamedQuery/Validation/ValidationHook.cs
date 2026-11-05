using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TheODI.NamedQuery.Validation;

internal static class ValidationHook
{
    private const string Marker = "NAMEDQUERY_VALIDATION";
    private const string SourcesVariable = "NAMEDQUERY_VALIDATION_SOURCES";
    private const string HostingListener = "Microsoft.Extensions.Hosting";
    private const string HostFailure = "NQ0002";
    private static readonly Uri PlaceholderSparqlEndpoint = new("http://localhost/sparql");

    private static int completed;
    private static int hostBuilt;
    private static Timer? timer;

    internal static void Initialize()
    {
        if (Environment.GetEnvironmentVariable(Marker) != "1")
        {
            return;
        }

        Environment.SetEnvironmentVariable("DOTNET_STARTUP_HOOKS", null);

        var timeout = TimeSpan.FromSeconds(60);

        AppDomain.CurrentDomain.UnhandledException += (_, e) => Exit($"The application failed: {Message(e.ExceptionObject as Exception)}");
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Fail("The application exited before the host started.");
        timer = new Timer(_ => Exit($"Timed out after {timeout.TotalSeconds} seconds waiting for the host to start."), null, timeout, Timeout.InfiniteTimeSpan);

        DiagnosticListener.AllListeners.Subscribe(new Observer<DiagnosticListener>(listener =>
        {
            if (listener.Name == HostingListener)
            {
                listener.Subscribe(new Observer<KeyValuePair<string, object?>>(OnHostingEvent));
            }
        }));
    }

    private static void OnHostingEvent(KeyValuePair<string, object?> e)
    {
        switch (e)
        {
            case { Key: "HostBuilding", Value: IHostBuilder builder }:
                Configure(builder);
                break;
            case { Key: "HostBuilt", Value: IHost host } when Interlocked.Exchange(ref hostBuilt, 1) == 0:
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                lifetime.ApplicationStarted.Register(() => Validate(host.Services, lifetime));
                break;
        }
    }

    private static void Configure(IHostBuilder builder) => builder
        .ConfigureLogging(logging => logging.ClearProviders())
        .ConfigureServices(services => services
            .AddSingleton<IServer, NoopServer>()
            .AddSingleton<IHostLifetime, NoopHostLifetime>()
            .PostConfigure<NamedQueryOptions>(options =>
            {
                if (options.SparqlEndpoint is not { IsAbsoluteUri: true })
                {
                    options.SparqlEndpoint = PlaceholderSparqlEndpoint;
                }
            }));

    private static void Validate(IServiceProvider services, IHostApplicationLifetime lifetime)
    {
        if (Interlocked.Exchange(ref completed, 1) != 0)
        {
            return;
        }

        try
        {
            if (services.GetService<Resources>() is null)
            {
                return;
            }

            var definitions = services.GetRequiredService<IOptions<NamedQueryOptions>>().Value.Definitions;
            var locator = new SourceLocator(Environment.GetEnvironmentVariable(SourcesVariable), AssemblyPath, definitions);
            foreach (var diagnostic in Validator.Validate(services))
            {
                Console.Out.WriteLine(locator.Locate(diagnostic));
            }
        }
        catch (Exception e)
        {
            Console.Out.WriteLine(HostError($"Validation failed: {Message(e)}"));
        }
        finally
        {
            Console.Out.Flush();
            lifetime.StopApplication();
        }
    }

    private static void Exit(string message)
    {
        Fail(message);
        Environment.Exit(0);
    }

    private static void Fail(string message)
    {
        if (Interlocked.Exchange(ref completed, 1) == 0)
        {
            Console.Out.WriteLine(HostError(message));
            Console.Out.Flush();
        }
    }

    private static string AssemblyPath => Assembly.GetEntryAssembly()?.Location ?? string.Empty;

    private static Diagnostic HostError(string message) => new(HostFailure, AssemblyPath, message);

    private static string Message(Exception? e) => e switch
    {
        null => "Unknown error.",
        TargetInvocationException or AggregateException when e.InnerException is { } inner => Message(inner),
        _ => e.Message.ReplaceLineEndings(" ")
    };

    private sealed class Observer<T>(Action<T> next) : IObserver<T>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(T value) => next(value);
    }
}
