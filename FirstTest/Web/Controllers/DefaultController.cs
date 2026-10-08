using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Route("/{**path}")]
[AllowSynchronousIO]
[ResolveEndpoint(Order = 0)]
[RequireEndpoint(Order = 1)]
[ProcessQuery(Order = 2)]
[SetContent(Order = 3)]
public class DefaultController
{
    [HttpGet]
    public void Get() { }
}
