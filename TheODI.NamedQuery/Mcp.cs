using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Text.Json.Nodes;
using TheODI.NamedQuery.Filters;
using TheODI.NamedQuery.Model;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using StringWriter = VDS.RDF.Writing.StringWriter;

namespace TheODI.NamedQuery;

internal class Mcp(Resources resources, QueryService queryService)
{
    internal const string Route = "/mcp";

    private static readonly IRdfWriter graphWriter = new CompressingTurtleWriter();
    private static readonly ISparqlResultsWriter resultsWriter = new SparqlHtmlWriter();

    internal ValueTask<ListToolsResult> ListTools(RequestContext<ListToolsRequestParams> _, CancellationToken __) =>
        ValueTask.FromResult(new ListToolsResult
        {
            Tools = [.. resources.Manifest.Node.Endpoint.Select(Tool)],
        });

    internal async ValueTask<CallToolResult> CallTool(RequestContext<CallToolRequestParams> request, CancellationToken ct)
    {
        var name = request.Params!.Name;
        var endpoint = resources.Manifest.Node.Endpoint.SingleOrDefault(endpoint => endpoint.ToolName == name)
            ?? throw new McpProtocolException($"Unknown tool: {name}", McpErrorCode.InvalidParams);

        var arguments = (request.Params.Arguments ?? new Dictionary<string, JsonElement>()).ToDictionary(
            static argument => argument.Key,
            static argument => argument.Value.ValueKind == JsonValueKind.String ? argument.Value.GetString()! : argument.Value.GetRawText());

        var missing = endpoint.Parameters.Select(static parameter => parameter.Name).Where(name => !arguments.ContainsKey(name)).ToList();
        if (missing.Count != 0)
        {
            return Error($"Missing required arguments: {string.Join(", ", missing)}");
        }

        try
        {
            return Text(Serialize(await queryService.ExecuteAsync(endpoint, queryService.Parametrize(endpoint, arguments), ct)));
        }
        catch (Exception e) when (e is RdfQueryException or HttpRequestException)
        {
            return Error($"SPARQL endpoint failed: {string.Join(" ---> ", e.Messages())}");
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            return Error($"SPARQL endpoint timed out: {string.Join(" ---> ", e.Messages())}");
        }
    }

    private static JsonObject Schema(Parameter parameter) => new()
    {
        ["type"] = parameter.JsonSchemaType.ToString().ToLowerInvariant(),
        ["description"] = parameter.Description,
        ["examples"] = new JsonArray(parameter.Example),
    };

    private static string Serialize(object result) => result switch
    {
        Response response => StringWriter.Write(response.Graph, graphWriter),
        SparqlResultSet results => StringWriter.Write(results, resultsWriter),
        _ => throw new InvalidOperationException($"Unknown result type {result.GetType()}"),
    };

    private static CallToolResult Text(string text) => new()
    {
        Content = [new TextContentBlock { Text = text }],
    };

    private static CallToolResult Error(string text) => new()
    {
        Content = [new TextContentBlock { Text = text }],
        IsError = true,
    };

    private Tool Tool(Model.Endpoint endpoint) => new()
    {
        Name = endpoint.ToolName,
        Title = endpoint.Name,
        Description = $"""
            {endpoint.Description}

            Underlying SPARQL query:
            ```sparql
            {resources.Query(endpoint).Trim()}
            ```
            """,
        InputSchema = JsonSerializer.SerializeToElement(new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject(endpoint.Parameters.Select(static parameter => KeyValuePair.Create(parameter.Name, (JsonNode?)Schema(parameter)))),
            ["required"] = new JsonArray([.. endpoint.Parameters.Select(static parameter => (JsonNode?)parameter.Name)]),
        }),
        Annotations = new()
        {
            ReadOnlyHint = true,
        },
    };
}
