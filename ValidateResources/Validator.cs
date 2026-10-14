extern alias App;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Shacl;
using Web;
using Web.Controllers;
using AppProgram = App::Program;

namespace ValidateResources;

internal static class Validator
{
    private const string ManifestName = "manifest.ttl";
    private const string ShapesName = "ValidateResources.shapes.ttl";
    private static readonly WebApplicationFactory<AppProgram> factory = Factory();
    private static readonly Resources resources = factory.Services.GetRequiredService<Resources>();

    internal static IEnumerable<Diagnostic> Validate() =>
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

    private static IEnumerable<Diagnostic> ManifestExists()
    {
        if (resources.Info(ManifestName) is null)
        {
            yield return new Diagnostic(ManifestName, "Manifest file not found in endpoint definition folder");
        }
    }

    private static IEnumerable<Diagnostic> ManifestIsValidTurtle()
    {
        if (resources.Reader(ManifestName) is not { } reader)
        {
            return [];
        }

        try
        {
            new TurtleParser().Load(new NullHandler(), reader);
            return [];
        }
        catch (RdfParseException e)
        {
            return [ParseError(ManifestName, $"Manifest is not valid Turtle: {e.Message}", e)];
        }
    }

    private static IEnumerable<Diagnostic> ManifestConformsToShapes()
    {
        if (ManifestGraph() is not { } dataGraph)
        {
            return [];
        }

        return Shapes().Validate(dataGraph).Results
            .Select(result => new Diagnostic(ManifestName, $"Manifest does not conform to shapes: {result.FocusNode}: {result.Message?.Value ?? result.SourceConstraintComponent.ToString()}"));
    }

    private static IEnumerable<Diagnostic> SparqlFilesExist()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        return resources.Manifest.Endpoints
            .Select(endpoint => endpoint.SparqlResourceName)
            .Where(path => resources.Info(path) is null)
            .Select(path => new Diagnostic(ManifestName, $"Endpoint SPARQL file not found: {path}"));
    }

    private static IEnumerable<Diagnostic> SparqlFilesValid()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        return resources.Manifest.Endpoints
            .Select(endpoint => endpoint.SparqlResourceName)
            .SelectMany(SparqlFileValid);
    }

    private static IEnumerable<Diagnostic> SparqlFileValid(string path)
    {
        if (resources.Reader(path) is not { } reader)
        {
            return [];
        }

        try
        {
            new SparqlQueryParser().Parse(reader);
            return [];
        }
        catch (RdfParseException e)
        {
            return [ParseError(path, $"Endpoint SPARQL is not valid SPARQL: {e.Message}", e)];
        }
    }

    private static WebApplicationFactory<AppProgram> Factory()
    {
        Environment.SetEnvironmentVariable($"ASPNETCORE_TEST_CONTENTROOT_{typeof(AppProgram).Assembly.GetName().Name!.ToUpperInvariant().Replace('.', '_')}", AppContext.BaseDirectory);

        return new WebApplicationFactory<AppProgram>().WithWebHostBuilder(builder => builder
            .UseSetting("NamedQuery:SparqlEndpoint", "http://localhost/")
            .ConfigureLogging(logging => logging.ClearProviders()));
    }

    private static IEnumerable<Diagnostic> EndpointPathsDoNotCollideWithWebRoutes()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(route => route.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo != typeof(EndpointController))
            .ToList();

        return resources.Manifest.Endpoints
            .Select(endpoint => endpoint.Path)
            .SelectMany(path => routes
                .Where(route => new TemplateMatcher(new RouteTemplate(route.RoutePattern), new RouteValueDictionary(route.RoutePattern.Defaults)).TryMatch($"/{path}", []))
                .Select(route => new Diagnostic(ManifestName, $"Endpoint path collides with built-in route: {path} ({route.DisplayName})")));
    }

    private static IEnumerable<Diagnostic> ParameterNamesDoNotCollideWithFormatFilter()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        var formatFilters = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Single(route => route.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo == typeof(EndpointController))
            .Metadata
            .OfType<FormatFilterAttribute>()
            .Select(attribute => (FormatFilter)attribute.CreateInstance(factory.Services))
            .ToList();

        return resources.Manifest.Endpoints
            .SelectMany(endpoint => endpoint.Parameters
                .Where(parameter => formatFilters.Any(filter => ReadsAsFormat(filter, parameter.Name)))
                .Select(parameter => new Diagnostic(ManifestName, $"Endpoint parameter collides with format query string parameter: {parameter.Name} ({endpoint.Path})")));
    }

    private static IEnumerable<Diagnostic> ToolNamesDoNotCollide()
    {
        if (ConformingManifestGraph() is null)
        {
            return [];
        }

        return resources.Manifest.Endpoints
            .GroupBy(endpoint => endpoint.ToolName)
            .Where(group => group.Count() > 1)
            .Select(group => new Diagnostic(ManifestName, $"Endpoint paths collide as MCP tool name: {group.Key} ({string.Join(", ", group.Select(endpoint => endpoint.Path))})"));
    }

    private static bool ReadsAsFormat(FormatFilter filter, string name)
    {
        var probe = Guid.NewGuid().ToString();
        var http = new DefaultHttpContext();
        http.Request.QueryString = QueryString.Create(name, probe);

        return filter.GetFormat(new ActionContext(http, new RouteData(), new ActionDescriptor())) == probe;
    }

    private static Graph? ManifestGraph()
    {
        if (resources.Reader(ManifestName) is not { } reader)
        {
            return null;
        }

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

    private static Graph? ConformingManifestGraph() => ManifestGraph() is { } graph && Shapes().Validate(graph).Conforms ? graph : null;

    private static ShapesGraph Shapes()
    {
        var shapesGraph = new ShapesGraph(new Graph());
        using var shapesReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ShapesName)!);
        new TurtleParser().Load(shapesGraph, shapesReader);
        return shapesGraph;
    }

    private static Diagnostic ParseError(string origin, string text, RdfParseException e) => new(origin, text)
    {
        Line = e.HasPositionInformation ? e.StartLine : null,
        Column = e.HasPositionInformation ? e.StartPosition : null,
        EndLine = e.HasPositionInformation ? e.EndLine : null,
        EndColumn = e.HasPositionInformation ? e.EndPosition : null
    };
}
