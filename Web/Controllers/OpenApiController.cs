using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using System.Net.Mime;
using System.Text.Json.Nodes;
using VDS.RDF;
using VDS.RDF.Query;
using Web.Filters;
using Web.Formatters;
using Web.Model;

namespace Web.Controllers;

[AllowSynchronousIO]
public class OpenApiController(Resources resources)
{
    internal const string Route = "/openapi.json";
    internal const string Name = "";

    private const string GraphResponse = "graph";
    private const string ResultsResponse = "results";
    private const string ProblemResponse = "problem";
    private const string UpstreamResponse = "upstream";
    private const string TimeoutResponse = "timeout";

    [HttpGet(Route, Name = nameof(GetJson))]
    [Produces(OpenApiJsonFormatter.MediaType)]
    public OpenApiDocument GetJson() => Get();

    [HttpGet("/openapi.yaml", Name = nameof(GetYaml))]
    [Produces(OpenApiYamlFormatter.MediaType)]
    public OpenApiDocument GetYaml() => Get();

    private OpenApiDocument Get() => new()
    {
        Info = new()
        {
            Title = resources.Manifest.Node.Name,
            Version = "1", // TODO: Don't hardcode
        },
        Paths = Paths(),
        Components = new()
        {
            Responses = new Dictionary<string, IOpenApiResponse>
            {
                [GraphResponse] = Response(
                    "Graph",
                    "RDF graph produced by a CONSTRUCT or DESCRIBE query, serialised in the negotiated format. JSON-LD is framed when the endpoint defines a frame.",
                    [.. GraphFormatter.MediaTypes, .. MimeTypesHelper.JsonLd]),
                [ResultsResponse] = Response(
                    "SPARQL results",
                    "Results of a SELECT or ASK query, serialised in the negotiated SPARQL results format.",
                    SparqlFormatter.MediaTypes),
                [ProblemResponse] = Response(
                    "Invalid parameter",
                    "Validation problem details listing each required query string parameter that was not supplied or was repeated.",
                    [MediaTypeNames.Application.ProblemJson]),
                [UpstreamResponse] = Response(
                    "SPARQL endpoint failed",
                    "Problem details reporting that the underlying SPARQL endpoint could not be reached or returned an error.",
                    [MediaTypeNames.Application.ProblemJson]),
                [TimeoutResponse] = Response(
                    "SPARQL endpoint timed out",
                    "Problem details reporting that the underlying SPARQL endpoint did not respond in time.",
                    [MediaTypeNames.Application.ProblemJson]),
            },
        },
    };

    private OpenApiPaths Paths()
    {
        var paths = new OpenApiPaths();

        foreach (var endpoint in resources.Manifest.Node.Endpoint)
        {
            paths.Add($"/{endpoint.Path}", new OpenApiPathItem
            {
                Operations = new()
                {
                    [HttpMethod.Get] = Operation(endpoint),
                },
            });
        }

        return paths;
    }

    private OpenApiOperation Operation(Model.Endpoint endpoint)
    {
        var responses = new OpenApiResponses
        {
            ["200"] = resources.QueryType(endpoint) switch
            {
                SparqlQueryType.Construct or
                SparqlQueryType.Describe or
                SparqlQueryType.DescribeAll => new OpenApiResponseReference(GraphResponse),

                _ => new OpenApiResponseReference(ResultsResponse),
            },
            ["502"] = new OpenApiResponseReference(UpstreamResponse),
            ["504"] = new OpenApiResponseReference(TimeoutResponse),
        };

        if (endpoint.Parameters.Count != 0)
        {
            responses["400"] = new OpenApiResponseReference(ProblemResponse);
        }

        return new()
        {
            Summary = endpoint.Name,
            Description = $"""
                {endpoint.Description}

                Underlying SPARQL query:
                ```sparql
                {resources.Query(endpoint).Trim()}
                ```
                """,
            Parameters = [.. endpoint.Parameters.Select(Parameter)],
            Responses = responses,
        };
    }

    private OpenApiParameter Parameter(Model.Parameter parameter) => new()
    {
        Name = parameter.Name,
        Description = parameter.Description,
        In = ParameterLocation.Query,
        Required = true,
        Schema = new OpenApiSchema
        {
            Type = parameter.JsonSchemaType,
        },
        Example = JsonValue.Create(parameter.Example),
    };

    private static OpenApiResponse Response(string summary, string description, IEnumerable<string> mediaTypes) => new()
    {
        Summary = summary,
        Description = description,
        Content = mediaTypes.Distinct().ToDictionary(mediaType => mediaType, _ => (IOpenApiMediaType)new OpenApiMediaType()),
    };
}
