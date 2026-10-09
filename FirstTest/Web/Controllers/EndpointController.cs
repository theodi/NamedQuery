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
    [ParametrizeSparql(Order = 4)]
    [ExecuteSparql(Order = 5)]
    [Respond200QueryResult(Order = 6)]
    public void Get() { }
}
