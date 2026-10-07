using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace Web.Controllers;

[Route("/")]
public class DefaultController(ISparqlQueryClient sparql)
{
    [HttpGet]
    public async IAsyncEnumerable<string> GetAsync([EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var endpoint in Resources.Manifest.Endpoints)
        {
            var results = await sparql.QueryWithResultSetAsync(endpoint.Query, ct);

            yield return VDS.RDF.Writing.StringWriter.Write(results, new SparqlJsonWriter());
        }
    }
}
