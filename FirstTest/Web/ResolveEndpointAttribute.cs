using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web;

internal class ResolveEndpointAttribute : TypeFilterAttribute<ResolveEndpointAttribute.Filter>
{
    internal class Filter(EndpointContext endpointContext) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext context)
        {
            if (context.RouteData.Values["path"] is string path)
            {
                endpointContext.Endpoint = Resources.Manifest[path];
            }
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext context) { }
    }
}
