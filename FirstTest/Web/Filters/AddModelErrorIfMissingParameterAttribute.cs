using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Model;

namespace Web.Filters;

internal class AddModelErrorIfMissingParameterAttribute : TypeFilterAttribute<AddModelErrorIfMissingParameterAttribute.Filter>
{
    internal class Filter(Context context) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext action)
        {
            var missing = context.Endpoint!.Parameters
                .Select(parameter => parameter.Name)
                .OfType<string>()
                .Where(name => !action.HttpContext.Request.Query.ContainsKey(name));

            foreach (var name in missing)
            {
                action.ModelState.AddModelError(name, "This query string parameter is required");
            }
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext _) { }
    }
}
