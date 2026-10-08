using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Model;

namespace Web.Filters;

internal class Respond404IfMissingEndpointAttribute : TypeFilterAttribute<Respond404IfMissingEndpointAttribute.Filter>
{
    internal class Filter(Context context) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext action)
        {
            if (context.Endpoint is null)
            {
                action.Result = new NotFoundResult();
            }
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext _) { }
    }
}
