using System.Net;

namespace Test;

internal class FailingSparqlStub(HttpStatusCode? status) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        status is { } code
            ? Task.FromResult(new HttpResponseMessage(code))
            : throw new HttpRequestException("Connection refused", new IOException("Socket closed"));
}
