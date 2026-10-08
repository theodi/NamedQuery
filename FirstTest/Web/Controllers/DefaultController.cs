using Microsoft.AspNetCore.Mvc;
using VDS.RDF.Query;

namespace Web.Controllers;

[Route("/{**path}")]
[AllowSynchronousIO]
public class DefaultController(ISparqlQueryClient sparql) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(string path, CancellationToken ct)
    {
        if (Resources.Manifest[path] is not { } endpoint)
        {
            return NotFound();
        }

        object results = endpoint.QueryType switch
        {
            SparqlQueryType.Construct or
            SparqlQueryType.Describe or
            SparqlQueryType.DescribeAll => new Model.ResponseContainer
            {
                Graph = await sparql.QueryWithResultGraphAsync(endpoint.Query, ct),
                Frame = endpoint.JsonLdFrame
            },

            _ => await sparql.QueryWithResultSetAsync(endpoint.Query, ct),
        };

        return Ok(results);
    }
}
