using System.Text;

namespace Test;

internal class SparqlStub : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage
        {
            Content = request.Headers.Accept.ToString().Contains("sparql-results")
                ? new StringContent("""{"head":{},"boolean":true}""", Encoding.UTF8, "application/sparql-results+json")
                : new StringContent("<urn:example:s> <urn:example:p> <urn:example:o> .", Encoding.UTF8, "text/turtle"),
        });
}
