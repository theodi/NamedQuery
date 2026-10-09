using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web.Filters;

public class AllowSynchronousIOAttribute : ResultFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context) =>
        context.HttpContext.Features.Get<IHttpBodyControlFeature>()?.AllowSynchronousIO = true;
}
