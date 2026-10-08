using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web;

public class AllowSynchronousIOAttribute : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context) =>
        context.HttpContext.Features.Get<IHttpBodyControlFeature>()?.AllowSynchronousIO = true;
}
