using Microsoft.AspNetCore.Mvc;
using Web.Filters;

namespace Web.Controllers;

[Route(EndpointRoute.Template)]
[AllowSynchronousIO]
[FormatFilter]
public class EndpointController
{
    [HttpGet]
    [AddModelErrorIfMissingParameter(Order = 0)]
    [Respond400IfInvalidModelState(Order = 1)]
    [ParametrizeSparql(Order = 2)]
    [ExecuteSparql(Order = 3)]
    [Respond200QueryResult(Order = 4)]
    public void Get() { }
}
