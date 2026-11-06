using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing.Template;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheODI.NamedQuery.Controllers;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Shacl;

namespace TheODI.NamedQuery.Validation;

internal class Validator(IServiceProvider services)
{
    private const string ManifestName = "manifest.ttl";
    private const string ShapesName = "TheODI.NamedQuery.Validation.shapes.ttl";

    private readonly Resources resources = services.GetRequiredService<Resources>();

    internal static IReadOnlyList<Diagnostic> Validate(IServiceProvider services)
    {
        try
        {
            return new Validator(services).Validate().ToList();
        }
        catch (Exception e)
        {
            return [new Diagnostic(DiagnosticCodes.Internal, "", e.Message)];
        }
    }

    private static bool ReadsAsFormat(FormatFilter filter, string name)
    {
        var probe = Guid.NewGuid().ToString();
        var http = new DefaultHttpContext();
        http.Request.QueryString = QueryString.Create(name, probe);

        return filter.GetFormat(new ActionContext(http, new RouteData(), new ActionDescriptor())) == probe;
    }

    private static ShapesGraph Shapes()
    {
        var shapesGraph = new ShapesGraph(new Graph());
        using var shapesReader = new StreamReader(typeof(Validator).Assembly.GetManifestResourceStream(ShapesName)!);
        new TurtleParser().Load(shapesGraph, shapesReader);
        return shapesGraph;
    }

    private static (int Line, int Column) Position(string text, int index)
    {
        var line = 1;
        var start = 0;
        for (var i = 0; i < index; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                start = i + 1;
            }
        }

        return (line, index - start + 1);
    }

    private static Diagnostic ParseError(string code, string path, string message, RdfParseException e) => new(code, path, message)
    {
        Line = e.HasPositionInformation ? e.StartLine : null,
        Column = e.HasPositionInformation ? e.StartPosition : null,
        EndLine = e.HasPositionInformation ? e.EndLine : null,
        EndColumn = e.HasPositionInformation ? e.EndPosition : null
    };

    private IEnumerable<Diagnostic> Validate() =>
    [
        .. ManifestExists(),
        .. ManifestIsValidTurtle(),
        .. ManifestConformsToShapes(),
        .. SparqlFilesExist(),
        .. SparqlFilesValid(),
        .. FrameFilesExist(),
        .. FrameFilesValid(),
        .. EndpointPathsDoNotCollideWithWebRoutes(),
        .. ParameterNamesDoNotCollideWithFormatFilter(),
        .. ToolNamesDoNotCollide(),
    ];

    private IEnumerable<Diagnostic> ManifestExists()
    {
        if (resources.Info(ManifestName) is null)
        {
            yield return new Diagnostic(DiagnosticCodes.ManifestMissing, ManifestName, "Manifest file not found in endpoint definition folder");
        }
    }

    private IEnumerable<Diagnostic> ManifestIsValidTurtle()
    {
        if (resources.Reader(ManifestName) is not { } reader)
        {
            return [];
        }

        using (reader)
        {
            try
            {
                new TurtleParser().Load(new NullHandler(), reader);
                return [];
            }
            catch (RdfParseException e)
            {
                return [ParseError(DiagnosticCodes.TurtleSyntax, ManifestName, $"Manifest is not valid Turtle: {e.Message}", e)];
            }
        }
    }

    private IEnumerable<Diagnostic> ManifestConformsToShapes()
    {
        if (ManifestGraph() is not { } dataGraph)
        {
            return [];
        }

        return Shapes().Validate(dataGraph).Results
            .Select(result => Located(DiagnosticCodes.ShapeViolation, ManifestName, $"Manifest does not conform to shapes: {result.FocusNode}: {result.Message?.Value ?? result.SourceConstraintComponent.ToString()}", result.ResultValue is ILiteralNode literal ? literal.Value : null));
    }

    private IEnumerable<Diagnostic> SparqlFilesExist()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        return resources.Manifest.Node.Endpoint
            .Select(endpoint => endpoint.SparqlResourceName)
            .Where(path => resources.Info(path) is null)
            .Select(path => Located(DiagnosticCodes.SparqlMissing, ManifestName, $"Endpoint SPARQL file not found: {path}", path));
    }

    private IEnumerable<Diagnostic> SparqlFilesValid()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        return resources.Manifest.Node.Endpoint
            .Select(endpoint => endpoint.SparqlResourceName)
            .SelectMany(SparqlFileValid);
    }

    private IEnumerable<Diagnostic> SparqlFileValid(string path)
    {
        if (resources.Reader(path) is not { } reader)
        {
            return [];
        }

        using (reader)
        {
            try
            {
                new SparqlQueryParser().Parse(reader);
                return [];
            }
            catch (RdfParseException e)
            {
                return [ParseError(DiagnosticCodes.SparqlSyntax, path, $"Endpoint SPARQL is not valid SPARQL: {e.Message}", e)];
            }
        }
    }

    private IEnumerable<Diagnostic> FrameFilesExist()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        return FrameNames()
            .Where(name => resources.Info(name) is null)
            .Select(name => Located(DiagnosticCodes.FrameMissing, ManifestName, $"Endpoint frame file not found: {name}", name));
    }

    private IEnumerable<Diagnostic> FrameFilesValid()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        return FrameNames().SelectMany(FrameFileValid);
    }

    private IEnumerable<Diagnostic> FrameFileValid(string name)
    {
        if (resources.Reader(name) is not { } reader)
        {
            return [];
        }

        using (reader)
        {
            try
            {
                using var json = new JsonTextReader(reader);
                JToken.ReadFrom(json);

                return [];
            }
            catch (JsonReaderException e)
            {
                return [new Diagnostic(DiagnosticCodes.FrameSyntax, name, $"Endpoint frame is not valid JSON: {e.Message}")
                {
                    Line = e.LineNumber,
                    Column = e.LinePosition
                }];
            }
        }
    }

    private IEnumerable<string> FrameNames() => resources.Manifest.Node.Endpoint
        .Select(endpoint => endpoint.FrameResourceName)
        .OfType<string>()
        .Distinct();

    private IEnumerable<Diagnostic> EndpointPathsDoNotCollideWithWebRoutes()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        var routes = services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(route => route.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo != typeof(EndpointController))
            .Where(AcceptsGetOrHead)
            .ToList();

        return resources.Manifest.Node.Endpoint
            .Select(endpoint => endpoint.Path)
            .SelectMany(path => routes
                .Where(route => new TemplateMatcher(new RouteTemplate(route.RoutePattern), new RouteValueDictionary(route.RoutePattern.Defaults)).TryMatch($"/{path}", []))
                .Select(route => Located(DiagnosticCodes.RouteCollision, ManifestName, $"Endpoint path collides with route: {path} ({route.DisplayName})", $"\"{path}\"")));
    }

    private IEnumerable<Diagnostic> ParameterNamesDoNotCollideWithFormatFilter()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        var formatFilters = services.GetRequiredService<EndpointDataSource>().Endpoints
            .Single(route => route.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo == typeof(EndpointController))
            .Metadata
            .OfType<FormatFilterAttribute>()
            .Select(attribute => (FormatFilter)attribute.CreateInstance(services))
            .ToList();

        return resources.Manifest.Node.Endpoint
            .SelectMany(endpoint => endpoint.Parameters
                .Where(parameter => formatFilters.Any(filter => ReadsAsFormat(filter, parameter.Name)))
                .Select(parameter => Located(DiagnosticCodes.FormatParameterCollision, ManifestName, $"Endpoint parameter collides with format query string parameter: {parameter.Name} ({endpoint.Path})", $"\"{parameter.Name}\"")));
    }

    private IEnumerable<Diagnostic> ToolNamesDoNotCollide()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        return resources.Manifest.Node.Endpoint
            .GroupBy(endpoint => endpoint.ToolName)
            .Where(group => group.Count() > 1)
            .Select(group => Located(DiagnosticCodes.ToolNameCollision, ManifestName, $"Endpoint paths collide as MCP tool name: {group.Key} ({string.Join(", ", group.Select(endpoint => endpoint.Path))})", $"\"{group.Last().Path}\""));
    }

    private bool AcceptsGetOrHead(RouteEndpoint route) =>
        route.Metadata.GetMetadata<IHttpMethodMetadata>() is not { } methods
        || methods.HttpMethods.Any(method => HttpMethods.IsGet(method) || HttpMethods.IsHead(method));

    private Graph? ManifestGraph()
    {
        if (resources.Reader(ManifestName) is not { } reader)
        {
            return null;
        }

        using (reader)
        {
            try
            {
                var graph = new Graph();
                new TurtleParser().Load(graph, reader);
                return graph;
            }
            catch (RdfParseException)
            {
                return null;
            }
        }
    }

    private Graph? ConformingManifestGraph() => ManifestGraph() is { } graph && Shapes().Validate(graph).Conforms ? graph : null;

    private Diagnostic Located(string code, string path, string message, string? needle)
    {
        var diagnostic = new Diagnostic(code, path, message);
        if (needle is null || resources.Reader(ManifestName) is not { } reader)
        {
            return diagnostic;
        }

        string text;
        using (reader)
        {
            text = reader.ReadToEnd();
        }

        var index = text.IndexOf(needle, StringComparison.Ordinal);
        if (index < 0)
        {
            return diagnostic;
        }

        var (line, column) = Position(text, index);
        var (endLine, endColumn) = Position(text, index + needle.Length);
        return diagnostic with { Line = line, Column = column, EndLine = endLine, EndColumn = endColumn };
    }
}
