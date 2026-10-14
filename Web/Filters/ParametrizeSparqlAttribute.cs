using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Model;

namespace Web.Filters;

internal class ParametrizeSparqlAttribute : TypeFilterAttribute<ParametrizeSparqlAttribute.Filter>
{
    internal class Filter(Context context, QueryService queryService) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext action)
        {
            var values = action.HttpContext.Request.Query.ToDictionary(
                static kv => kv.Key,
                static kv => kv.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            context.Query = queryService.Parametrize(context.Endpoint!, values);
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext _) { }
    }
}
