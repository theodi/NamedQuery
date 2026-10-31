using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TheODI.NamedQuery.Model;

namespace TheODI.NamedQuery.Filters;

internal class Respond200QueryResultAttribute : TypeFilterAttribute<Respond200QueryResultAttribute.Filter>
{
    internal class Filter(Context context) : IActionFilter
    {
        void IActionFilter.OnActionExecuted(ActionExecutedContext action) =>
            action.Result = new OkObjectResult(context.Result);

        void IActionFilter.OnActionExecuting(ActionExecutingContext _) { }
    }
}
