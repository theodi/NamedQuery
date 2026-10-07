using Microsoft.AspNetCore.Mvc;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace Web.Controllers;

[Route("/{**path}")]
public class DefaultController(ISparqlQueryClient sparql) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<string>> GetAsync(string path, CancellationToken ct)
    {
        if (Resources.Manifest[path] is not { } endpoint)
        {
            return NotFound();
        }

        var results = await sparql.QueryWithResultSetAsync(endpoint.Query, ct);

        return VDS.RDF.Writing.StringWriter.Write(results, new SparqlJsonWriter());
    }
}
