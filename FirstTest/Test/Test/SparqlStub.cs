using System.Text;

namespace Test;

internal class SparqlStub : HttpMessageHandler
{
    internal const string Response = """{"head":{},"boolean":true}""";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage { Content = new StringContent(Response, Encoding.UTF8, "application/sparql-results+json") });
}
