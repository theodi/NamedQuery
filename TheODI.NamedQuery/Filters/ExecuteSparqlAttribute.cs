using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TheODI.NamedQuery.Model;

namespace TheODI.NamedQuery.Filters;

internal class ExecuteSparqlAttribute : TypeFilterAttribute<ExecuteSparqlAttribute.Filter>
{
    internal class Filter(Context context, QueryService queryService) : IAsyncActionFilter
    {
        async Task IAsyncActionFilter.OnActionExecutionAsync(ActionExecutingContext action, ActionExecutionDelegate next)
        {
            context.Result = await queryService.ExecuteAsync(context.Endpoint!, context.Query!, action.HttpContext.RequestAborted);

            await next();
        }
    }
}
