using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace TheODI.NamedQuery.Validation;

internal sealed class NoopServer : IServer
{
    IFeatureCollection IServer.Features { get; } = new FeatureCollection();

    void IDisposable.Dispose()
    {
    }

    Task IServer.StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IServer.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
