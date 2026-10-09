using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Test;

internal class SparqlStub : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var parameters = request.Content is null
            ? QueryHelpers.ParseQuery(request.RequestUri!.Query)
            : QueryHelpers.ParseQuery(await request.Content.ReadAsStringAsync(cancellationToken));

        return new HttpResponseMessage
        {
            Content = request.Headers.Accept.ToString().Contains("sparql-results")
                ? new StringContent("""{"head":{},"boolean":true}""", Encoding.UTF8, "application/sparql-results+json")
                : new StringContent("<urn:example:s> <urn:example:p> <urn:example:o> .", Encoding.UTF8, "text/turtle"),
        };
    }
}
