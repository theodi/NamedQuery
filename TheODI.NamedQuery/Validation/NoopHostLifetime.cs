using Microsoft.Extensions.Hosting;

namespace TheODI.NamedQuery.Validation;

internal sealed class NoopHostLifetime : IHostLifetime
{
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
