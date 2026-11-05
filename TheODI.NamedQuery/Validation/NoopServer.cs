using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace TheODI.NamedQuery.Validation;

internal sealed class NoopServer : IServer
{
    public IFeatureCollection Features { get; } = new FeatureCollection();

    public void Dispose()
    {
    }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
