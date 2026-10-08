using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Model;

namespace Web.Filters;

internal class ResolveEndpointAttribute : TypeFilterAttribute<ResolveEndpointAttribute.Filter>
{
    internal class Filter(Context context) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext action)
        {
            if (action.RouteData.Values["path"] is string path)
            {
                context.Endpoint = Resources.Manifest[path];
            }
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext _) { }
    }
}
