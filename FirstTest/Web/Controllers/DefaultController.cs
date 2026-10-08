using Microsoft.AspNetCore.Mvc;
using VDS.RDF.Query;

namespace Web.Controllers;

[Route("/{**path}")]
[AllowSynchronousIO]
[ResolveEndpoint]
[RequireEndpoint]
public class DefaultController(ISparqlQueryClient sparql, EndpointContext endpointContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        var endpoint = endpointContext.Endpoint!;

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
