using Microsoft.AspNetCore.Mvc;
using Web.Filters;

namespace Web.Controllers;

[Route("/{**path}")]
[AllowSynchronousIO]
public class DefaultController
{
    [HttpGet]
    [ResolveEndpoint(Order = 0)]
    [RequireEndpoint(Order = 1)]
    [ProcessQuery(Order = 2)]
    [SetContent(Order = 3)]
    public void Get() { }
}
