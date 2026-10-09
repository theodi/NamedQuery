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
    [AddModelErrorIfMissingParameter(Order = 0)]
    [Respond400IfInvalidModelState(Order = 1)]
    [ParametrizeSparql(Order = 2)]
    [ExecuteSparql(Order = 3)]
    [Respond200QueryResult(Order = 4)]
    public void Get() { }
}
