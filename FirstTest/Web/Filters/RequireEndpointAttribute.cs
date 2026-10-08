using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web.Filters;

internal class RequireEndpointAttribute : TypeFilterAttribute<RequireEndpointAttribute.Filter>
{
    internal class Filter(EndpointContext endpointContext) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext context)
        {
            if (endpointContext.Endpoint is null)
            {
                context.Result = new NotFoundResult();
            }
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext context) { }
    }
}
