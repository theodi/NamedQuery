using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VDS.RDF;
using VDS.RDF.Query;
using Web.Model;

namespace Web.Filters;

internal class ExecuteSparqlAttribute : TypeFilterAttribute<ExecuteSparqlAttribute.Filter>
{
    internal class Filter(ISparqlQueryClient sparql, Context context) : IAsyncActionFilter
    {
        private static readonly NodeFactory factory = new();

        async Task IAsyncActionFilter.OnActionExecutionAsync(ActionExecutingContext action, ActionExecutionDelegate next)
        {
            var endpoint = context.Endpoint!;
            var query = Parametrize(endpoint, action.HttpContext.Request.Query);
            var ct = action.HttpContext.RequestAborted;

            context.Result = endpoint.QueryType switch
            {
                SparqlQueryType.Construct or
                SparqlQueryType.Describe or
                SparqlQueryType.DescribeAll => new Response
                {
                    Graph = await sparql.QueryWithResultGraphAsync(query, ct),
                    Frame = endpoint.JsonLdFrame
                },

                _ => await sparql.QueryWithResultSetAsync(query, ct),
            };

            await next();
        }

        private static string Parametrize(Model.Endpoint endpoint, IQueryCollection values)
        {
            var sparql = new SparqlParameterizedString(endpoint.Query);

            foreach (var parameter in endpoint.Parameters)
            {
                if (values.TryGetValue(parameter.Name, out var value))
                {
                    var node = factory.CreateLiteralNode(value.ToString(), parameter.Datatype);
                    sparql.SetVariable(parameter.Name, node);
                }
            }

            return sparql.ToString();
        }
    }
}
