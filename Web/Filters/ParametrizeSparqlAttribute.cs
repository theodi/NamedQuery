using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VDS.RDF;
using VDS.RDF.Query;
using Web.Model;

namespace Web.Filters;

internal class ParametrizeSparqlAttribute : TypeFilterAttribute<ParametrizeSparqlAttribute.Filter>
{
    internal class Filter(Context context, Resources resources) : IActionFilter
    {
        private static readonly NodeFactory factory = new();

        void IActionFilter.OnActionExecuting(ActionExecutingContext action)
        {
            var endpoint = context.Endpoint!;
            var values = action.HttpContext.Request.Query;
            var sparql = new SparqlParameterizedString(resources.Query(endpoint));

            foreach (var parameter in endpoint.Parameters)
            {
                if (values.TryGetValue(parameter.Name, out var value))
                {
                    var node = factory.CreateLiteralNode(value.ToString(), parameter.Datatype);
                    sparql.SetVariable(parameter.Name, node);
                }
            }

            context.Query = sparql.ToString();
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext _) { }
    }
}
