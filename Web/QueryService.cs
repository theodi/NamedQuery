using VDS.RDF;
using VDS.RDF.Query;
using Web.Model;

namespace Web;

internal class QueryService(ISparqlQueryClient client, Resources resources)
{
    private static readonly NodeFactory factory = new();

    internal string Parametrize(Model.Endpoint endpoint, IReadOnlyDictionary<string, string> values)
    {
        var sparql = new SparqlParameterizedString(resources.Query(endpoint));

        foreach (var parameter in endpoint.Parameters)
        {
            if (values.TryGetValue(parameter.Name, out var value))
            {
                var node = factory.CreateLiteralNode(value, parameter.Datatype);
                sparql.SetVariable(parameter.Name, node);
            }
        }

        return sparql.ToString();
    }

    internal async Task<object> ExecuteAsync(Model.Endpoint endpoint, string query, CancellationToken ct) =>
        resources.QueryType(endpoint) switch
        {
            SparqlQueryType.Construct or
            SparqlQueryType.Describe or
            SparqlQueryType.DescribeAll => new Response
            {
                Graph = await client.QueryWithResultGraphAsync(query, ct),
                Frame = resources.JsonLdFrame(endpoint)
            },

            _ => await client.QueryWithResultSetAsync(query, ct),
        };
}
