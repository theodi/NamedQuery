using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing.Template;
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

    private IEnumerable<Diagnostic> Validate() =>
    [
        .. ManifestExists(),
        .. ManifestIsValidTurtle(),
        .. ManifestConformsToShapes(),
        .. SparqlFilesExist(),
        .. SparqlFilesValid(),
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
            .Select(result => new Diagnostic(DiagnosticCodes.ShapeViolation, ManifestName, $"Manifest does not conform to shapes: {result.FocusNode}: {result.Message?.Value ?? result.SourceConstraintComponent.ToString()}"));
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
            .Select(path => new Diagnostic(DiagnosticCodes.SparqlMissing, ManifestName, $"Endpoint SPARQL file not found: {path}"));
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
                .Select(route => new Diagnostic(DiagnosticCodes.RouteCollision, ManifestName, $"Endpoint path collides with built-in route: {path} ({route.DisplayName})")));
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
                .Select(parameter => new Diagnostic(DiagnosticCodes.FormatParameterCollision, ManifestName, $"Endpoint parameter collides with format query string parameter: {parameter.Name} ({endpoint.Path})")));
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
            .Select(group => new Diagnostic(DiagnosticCodes.ToolNameCollision, ManifestName, $"Endpoint paths collide as MCP tool name: {group.Key} ({string.Join(", ", group.Select(endpoint => endpoint.Path))})"));
    }

    private bool AcceptsGetOrHead(RouteEndpoint route) =>
        route.Metadata.GetMetadata<IHttpMethodMetadata>() is not { } methods
        || methods.HttpMethods.Any(method => HttpMethods.IsGet(method) || HttpMethods.IsHead(method));

    private bool ReadsAsFormat(FormatFilter filter, string name)
    {
        var probe = Guid.NewGuid().ToString();
        var http = new DefaultHttpContext();
        http.Request.QueryString = QueryString.Create(name, probe);

        return filter.GetFormat(new ActionContext(http, new RouteData(), new ActionDescriptor())) == probe;
    }

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

    private ShapesGraph Shapes()
    {
        var shapesGraph = new ShapesGraph(new Graph());
        using var shapesReader = new StreamReader(typeof(Validator).Assembly.GetManifestResourceStream(ShapesName)!);
        new TurtleParser().Load(shapesGraph, shapesReader);
        return shapesGraph;
    }

    private Diagnostic ParseError(string code, string path, string message, RdfParseException e) => new(code, path, message)
    {
        Line = e.HasPositionInformation ? e.StartLine : null,
        Column = e.HasPositionInformation ? e.StartPosition : null,
        EndLine = e.HasPositionInformation ? e.EndLine : null,
        EndColumn = e.HasPositionInformation ? e.EndPosition : null
    };
}
