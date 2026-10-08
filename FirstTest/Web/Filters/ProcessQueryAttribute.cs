using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VDS.RDF.Query;

namespace Web.Filters;

internal class ProcessQueryAttribute : TypeFilterAttribute<ProcessQueryAttribute.Filter>
{
    internal class Filter(ISparqlQueryClient sparql, EndpointContext endpointContext) : IAsyncActionFilter
    {
        async Task IAsyncActionFilter.OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var endpoint = endpointContext.Endpoint!;
            var ct = context.HttpContext.RequestAborted;

            endpointContext.Result = endpoint.QueryType switch
            {
                SparqlQueryType.Construct or
                SparqlQueryType.Describe or
                SparqlQueryType.DescribeAll => new Model.ResponseContainer
                {
                    Graph = await sparql.QueryWithResultGraphAsync(endpoint.Query, ct),
                    Frame = endpoint.JsonLdFrame
                },

                _ => await sparql.QueryWithResultSetAsync(endpoint.Query, ct),
            };

            await next();
        }
    }
}
