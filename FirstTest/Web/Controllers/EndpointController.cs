using Microsoft.AspNetCore.Mvc;
using Web.Filters;

namespace Web.Controllers;

[Route("/{**path}")]
[AllowSynchronousIO]
[FormatFilter]
public class EndpointController
{
    [HttpGet]
    [PopulateEndpointInContext(Order = 0)]
    [Respond404IfMissingEndpoint(Order = 1)]
    [ExecuteSparql(Order = 2)]
    [Respond200QueryResult(Order = 3)]
    public void Get() { }
}
