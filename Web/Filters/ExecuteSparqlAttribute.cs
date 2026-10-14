using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VDS.RDF.Query;
using Web.Model;

namespace Web.Filters;

internal class ExecuteSparqlAttribute : TypeFilterAttribute<ExecuteSparqlAttribute.Filter>
{
    internal class Filter(ISparqlQueryClient sparql, Context context, Resources resources) : IAsyncActionFilter
    {
        async Task IAsyncActionFilter.OnActionExecutionAsync(ActionExecutingContext action, ActionExecutionDelegate next)
        {
            var endpoint = context.Endpoint!;
            var query = context.Query!;
            var ct = action.HttpContext.RequestAborted;

            context.Result = resources.QueryType(endpoint) switch
            {
                SparqlQueryType.Construct or
                SparqlQueryType.Describe or
                SparqlQueryType.DescribeAll => new Response
                {
                    Graph = await sparql.QueryWithResultGraphAsync(query, ct),
                    Frame = resources.JsonLdFrame(endpoint)
                },

                _ => await sparql.QueryWithResultSetAsync(query, ct),
            };

            await next();
        }
    }
}
