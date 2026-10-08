using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Shacl;
using Web;
using Web.Controllers;

namespace ValidateResources;

internal static class Validator
{
    private const string ManifestName = "manifest.ttl";
    private const string ShapesName = "ValidateResources.shapes.ttl";
    private static readonly Uri FakeBase = new("http://resources/");

    internal static void Validate()
    {
        ManifestExists();
        ManifestIsValidTurtle();
        ManifestConformsToShapes();
        SparqlIsRelativeUri();
        SparqlFilesExist();
        SparqlFilesValid();
        EndpointPathsDoNotCollideWithWebRoutes();
    }

    private static void ManifestExists()
    {
        if (Resources.Info("manifest.ttl") is null)
        {
            throw new MsBuildCanonicalErrorException(ManifestName, "Manifest file not found in endpoint definition folder");
        }
    }

    private static void ManifestIsValidTurtle()
    {
        try
        {
            new TurtleParser().Load(new NullHandler(), Resources.Reader("manifest.ttl"));
        }
        catch (RdfParseException e)
        {
            throw new MsBuildCanonicalErrorException(ManifestName, $"Manifest is not valid Turtle: {e.Message}", e)
            {
                Line = e.HasPositionInformation ? e.StartLine : null,
                Column = e.HasPositionInformation ? e.StartPosition : null,
                EndLine = e.HasPositionInformation ? e.EndLine : null,
                EndColumn = e.HasPositionInformation ? e.EndPosition : null
            };
        }
    }

    private static void ManifestConformsToShapes()
    {
        var dataGraph = new Graph();
        new TurtleParser().Load(dataGraph, Resources.Reader(ManifestName));

        var shapesGraph = new ShapesGraph(new Graph());
        using var shapesReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ShapesName)!);
        new TurtleParser().Load(shapesGraph, shapesReader);

        var report = shapesGraph.Validate(dataGraph);
        if (report.Conforms) return;

        var violations = report.Results.Select(result => $"{result.FocusNode}: {result.Message?.Value ?? result.SourceConstraintComponent.ToString()}");
        throw new MsBuildCanonicalErrorException(ManifestName, $"Manifest does not conform to shapes: {string.Join("; ", violations)}");
    }

    private static void SparqlIsRelativeUri()
    {
        using var graph = new Graph();
        new TurtleParser().Load(graph, Resources.Reader(ManifestName));

        var invalid = graph.GetTriplesWithPredicate(graph.CreateUriNode(new Uri("http://example.org/namedquery/sparql")))
            .Select(triple => triple.Object)
            .OfType<ILiteralNode>()
            .Select(literal => literal.Value)
            .Where(value => !Uri.TryCreate(value, UriKind.Relative, out _))
            .ToList();

        if (invalid.Count == 0) return;

        throw new MsBuildCanonicalErrorException(ManifestName, $"Endpoint SPARQL must be relative URI: {string.Join("; ", invalid)}");
    }

    private static void SparqlFilesExist()
    {
        using var graph = new Graph();
        new TurtleParser().Load(graph, Resources.Reader(ManifestName));

        var missing = graph.GetTriplesWithPredicate(graph.CreateUriNode(new Uri("http://example.org/namedquery/sparql")))
            .Select(triple => triple.Object)
            .OfType<ILiteralNode>()
            .Select(literal => literal.Value)
            .Select(value => new Uri(value, UriKind.Relative))
            .Select(relative => new Uri(FakeBase, relative))
            .Select(absolute => absolute.GetComponents(UriComponents.Path, UriFormat.Unescaped))
            .Where(path => Resources.Info(path) is null)
            .ToList();

        if (missing.Count == 0) return;

        throw new MsBuildCanonicalErrorException(ManifestName, $"Endpoint SPARQL file not found: {string.Join("; ", missing)}");
    }

    private static void SparqlFilesValid()
    {
        using var graph = new Graph();
        new TurtleParser().Load(graph, Resources.Reader(ManifestName));

        var paths = graph.GetTriplesWithPredicate(graph.CreateUriNode(new Uri("http://example.org/namedquery/sparql")))
            .Select(triple => triple.Object)
            .OfType<ILiteralNode>()
            .Select(literal => literal.Value)
            .Select(value => new Uri(value, UriKind.Relative))
            .Select(relative => new Uri(FakeBase, relative))
            .Select(absolute => absolute.GetComponents(UriComponents.Path, UriFormat.Unescaped));

        foreach (var path in paths)
        {
            try
            {
                new SparqlQueryParser().Parse(Resources.Reader(path));
            }
            catch (RdfParseException e)
            {
                throw new MsBuildCanonicalErrorException(path, $"Endpoint SPARQL is not valid SPARQL: {e.Message}", e)
                {
                    Line = e.HasPositionInformation ? e.StartLine : null,
                    Column = e.HasPositionInformation ? e.StartPosition : null,
                    EndLine = e.HasPositionInformation ? e.EndLine : null,
                    EndColumn = e.HasPositionInformation ? e.EndPosition : null
                };
            }
        }
    }

    private static void EndpointPathsDoNotCollideWithWebRoutes()
    {
        Environment.SetEnvironmentVariable($"ASPNETCORE_TEST_CONTENTROOT_{typeof(EndpointController).Assembly.GetName().Name!.ToUpperInvariant().Replace('.', '_')}", AppContext.BaseDirectory);

        using var factory = new WebApplicationFactory<EndpointController>().WithWebHostBuilder(builder => builder
            .UseSetting("Options:SparqlEndpoint", "http://localhost/")
            .ConfigureLogging(logging => logging.ClearProviders()));

        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(route => route.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo != typeof(EndpointController))
            .ToList();

        var collisions = Resources.Manifest.Endpoints
            .Select(endpoint => endpoint.Path)
            .OfType<string>()
            .SelectMany(path => routes
                .Where(route => new TemplateMatcher(new RouteTemplate(route.RoutePattern), new RouteValueDictionary(route.RoutePattern.Defaults)).TryMatch($"/{path}", []))
                .Select(route => $"{path} ({route.DisplayName})"))
            .ToList();

        if (collisions.Count == 0)
        {
            return;
        }

        throw new MsBuildCanonicalErrorException(ManifestName, $"Endpoint paths collide with built-in routes: {string.Join("; ", collisions)}");
    }
}
