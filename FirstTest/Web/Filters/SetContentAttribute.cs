using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web.Filters;

internal class SetContentAttribute : TypeFilterAttribute<SetContentAttribute.Filter>
{
    internal class Filter(EndpointContext endpointContext) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext context) { }

        void IActionFilter.OnActionExecuted(ActionExecutedContext context) => context.Result = new OkObjectResult(endpointContext.Result);
    }
}
