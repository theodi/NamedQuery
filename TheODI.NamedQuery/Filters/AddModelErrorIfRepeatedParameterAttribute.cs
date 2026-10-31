using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TheODI.NamedQuery.Model;

namespace TheODI.NamedQuery.Filters;

internal class AddModelErrorIfRepeatedParameterAttribute : TypeFilterAttribute<AddModelErrorIfRepeatedParameterAttribute.Filter>
{
    internal class Filter(Context context) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext action)
        {
            var repeated = context.Endpoint!.Parameters
                .Select(parameter => parameter.Name)
                .Where(name => action.HttpContext.Request.Query[name].Count > 1);

            foreach (var name in repeated)
            {
                action.ModelState.AddModelError(name, "This query string parameter must not be repeated");
            }
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext _) { }
    }
}
