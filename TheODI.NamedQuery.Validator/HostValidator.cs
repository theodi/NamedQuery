using System.Reflection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LibraryValidator = TheODI.NamedQuery.Validation.Validator;

namespace TheODI.NamedQuery.Validator;

internal static class HostValidator
{
    private const string HostFailure = "NQ0002";
    private static readonly Uri PlaceholderSparqlEndpoint = new("http://localhost/sparql");

    internal static IReadOnlyList<Diagnostic> Validate(Arguments arguments)
    {
        var assembly = Assembly.Load(AssemblyName.GetAssemblyName(arguments.Assembly));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var factory = HostFactoryResolver.ResolveHostFactory(
            assembly,
            arguments.Timeout,
            stopApplication: false,
            configureHostBuilder: Configure,
            entrypointCompleted: exception =>
            {
                started.TrySetException(exception ?? new InvalidOperationException("The entry point exited before the host started."));
                exited.TrySetResult();
            });

        if (factory is null)
        {
            return [HostError(arguments, $"No host could be resolved from the entry point of {assembly.GetName().Name}.")];
        }

        IHost host;
        try
        {
            host = (IHost)factory([$"--{HostDefaults.ApplicationKey}={assembly.GetName().Name}"]);
        }
        catch (Exception e)
        {
            return [HostError(arguments, $"The host failed to build: {Message(e)}")];
        }

        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        try
        {
            using (lifetime.ApplicationStarted.Register(() => started.TrySetResult()))
            {
                if (!started.Task.Wait(arguments.Timeout))
                {
                    return [HostError(arguments, $"Timed out after {arguments.Timeout.TotalSeconds} seconds waiting for the host to start.")];
                }
            }
        }
        catch (AggregateException e)
        {
            return [HostError(arguments, $"The host failed to start: {Message(e.InnerException ?? e)}")];
        }

        try
        {
            if (host.Services.GetService<Resources>() is null)
            {
                return [];
            }

            var definitions = host.Services.GetRequiredService<IOptions<NamedQueryOptions>>().Value.Definitions;
            var locator = new SourceLocator(arguments, definitions);
            return LibraryValidator.Validate(host.Services).Select(locator.Locate).ToList();
        }
        finally
        {
            lifetime.StopApplication();
            exited.Task.Wait(arguments.Timeout);
        }
    }

    private static void Configure(object builder) => ((IHostBuilder)builder)
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

    private static Diagnostic HostError(Arguments arguments, string message) => new(arguments.Assembly, message) { Code = HostFailure };

    private static string Message(Exception e) => (e is TargetInvocationException or AggregateException && e.InnerException is { } inner ? inner : e).Message.ReplaceLineEndings(" ");
}
