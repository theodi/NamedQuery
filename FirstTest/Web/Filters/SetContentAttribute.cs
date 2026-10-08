using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Model;

namespace Web.Filters;

internal class SetContentAttribute : TypeFilterAttribute<SetContentAttribute.Filter>
{
    internal class Filter(Context context) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext _) { }

        void IActionFilter.OnActionExecuted(ActionExecutedContext action) => action.Result = new OkObjectResult(context.Result);
    }
}
