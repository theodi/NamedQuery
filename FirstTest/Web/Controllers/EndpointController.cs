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
    [AddModelErrorIfMissingParameter(Order = 2)]
    [Respond400IfInvalidModelState(Order = 3)]
    [ExecuteSparql(Order = 4)]
    [Respond200QueryResult(Order = 5)]
    public void Get() { }
}
