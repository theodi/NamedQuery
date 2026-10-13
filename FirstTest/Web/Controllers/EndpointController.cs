using Microsoft.AspNetCore.Mvc;
using Web.Constraints;
using Web.Filters;

namespace Web.Controllers;

[Route($$"""/{**{{RouteParameter}}:{{ManifestEndpointConstraint.Name}}}""")]
[AllowSynchronousIO]
[FormatFilter]
public class EndpointController
{
    internal const string RouteParameter = "path";

    [HttpGet]

    [PopulateEndpointInContext]

    [AddModelErrorIfMissingParameter(Order = 0)]
    [Respond400IfInvalidModelState(Order = 1)]
    [ParametrizeSparql(Order = 2)]
    [ExecuteSparql(Order = 3)]
    [Respond200QueryResult(Order = 4)]
    public void Get() { }
}
