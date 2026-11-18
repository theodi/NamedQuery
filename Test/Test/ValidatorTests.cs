using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TheODI.NamedQuery.Controllers;
using TheODI.NamedQuery.Validation;

namespace Test;

[TestClass]
public sealed class ValidatorTests
{
    [TestMethod]
    public void ValidDefinitionsHaveNoDiagnostics()
    {
        Validate(Fixture("Valid")).Should().BeEmpty();
    }

    [TestMethod]
    public void MissingManifestIsReported()
    {
        Validate(new NullFileProvider()).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.ManifestMissing &&
            diagnostic.Path == "manifest.ttl");
    }

    [TestMethod]
    public void InvalidTurtleIsReportedWithPosition()
    {
        var diagnostics = Validate(Fixture("Invalid"));

        diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.TurtleSyntax &&
            diagnostic.Path == "manifest.ttl" &&
            diagnostic.Line != null &&
            diagnostic.Message.Contains("Manifest is not valid Turtle"));
    }

    [TestMethod]
    public void ShapeViolationIsReported()
    {
        using var directory = new TempDefinitions("""
            PREFIX : <http://example.org/namedquery/>

            [ :name "shapes" ; :endpoint [ :path "a" ] ] .
            """);

        Validate(directory.Provider).Should().Contain(diagnostic => diagnostic.Code == DiagnosticCodes.ShapeViolation);
    }

    [TestMethod]
    public void MissingSparqlFileIsReported()
    {
        using var directory = new TempDefinitions(Manifest("missing"));

        Validate(directory.Provider).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.SparqlMissing &&
            diagnostic.Message.Contains("q.sparql"));
    }

    [TestMethod]
    public void InvalidSparqlIsReported()
    {
        using var directory = new TempDefinitions(Manifest("invalid"), "SELECT WHERE {");

        Validate(directory.Provider).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.SparqlSyntax &&
            diagnostic.Path == "q.sparql");
    }

    [TestMethod]
    public void BuiltInRouteCollisionIsReported()
    {
        Validate(Fixture("Colliding")).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.RouteCollision &&
            diagnostic.Message.Contains($"Endpoint path collides with route:  ({typeof(OpenApiController).FullName}.{nameof(OpenApiController.Get)}"));
    }

    [TestMethod]
    public void MissingFrameIsReportedWithManifestPosition()
    {
        using var directory = new TempDefinitions(Manifest("framed", "./f.jsonld"), "SELECT * WHERE { ?s ?p ?o }");

        var diagnostic = Validate(directory.Provider).Should().ContainSingle().Subject;

        diagnostic.Code.Should().Be(DiagnosticCodes.FrameMissing);
        diagnostic.Path.Should().Be("manifest.ttl");
        diagnostic.Line.Should().Be(9);
        diagnostic.Column.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void InvalidFrameIsReportedWithPosition()
    {
        using var directory = new TempDefinitions(Manifest("framed", "./f.jsonld"), "SELECT * WHERE { ?s ?p ?o }", "{ \"a\": ");

        Validate(directory.Provider).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.FrameSyntax &&
            diagnostic.Path == "f.jsonld" &&
            diagnostic.Line != null);
    }

    [TestMethod]
    public void ValidFrameHasNoDiagnostics()
    {
        using var directory = new TempDefinitions(Manifest("framed", "./f.jsonld"), "SELECT * WHERE { ?s ?p ?o }", "{ \"@type\": \"x\" }");

        Validate(directory.Provider).Should().BeEmpty();
    }

    [TestMethod]
    public void MissingSparqlIsReportedWithManifestPosition()
    {
        using var directory = new TempDefinitions(Manifest("missing"));

        Validate(directory.Provider).Should().ContainSingle().Which.Line.Should().Be(8);
    }

    [TestMethod]
    public void ToolNameCollisionIsReported()
    {
        Validate(Fixture("ToolName")).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.ToolNameCollision &&
            diagnostic.Message.Contains("tool_name (tool/name, tool_name)"));
    }

    [TestMethod]
    public void ToolNameCollisionIsIgnoredWhenExcludedFromMcp()
    {
        using var directory = new TempDefinitions("""
            PREFIX : <http://example.org/namedquery/>
            PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

            [
                :name "tool name" ;
                :endpoint
                    [ :path "tool/name" ; :sparql "./q.sparql"^^xsd:anyURI ; :name "a" ; :description "a" ; :mcp false ],
                    [ :path "tool_name" ; :sparql "./q.sparql"^^xsd:anyURI ; :name "b" ; :description "b" ] ;
            ] .
            """, "ASK {}");

        Validate(directory.Provider).Should().BeEmpty();
    }

    [TestMethod]
    public void NonBooleanMcpFlagIsReported()
    {
        using var directory = new TempDefinitions("""
            PREFIX : <http://example.org/namedquery/>
            PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

            [ :name "m" ; :endpoint [ :path "m" ; :sparql "./q.sparql"^^xsd:anyURI ; :name "m" ; :description "m" ; :mcp "no" ] ] .
            """, "ASK {}");

        Validate(directory.Provider).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.ShapeViolation &&
            diagnostic.Message.Contains("Endpoint MCP flag must be xsd:boolean literal"));
    }

    [TestMethod]
    public void HostGetRouteCollisionIsReported()
    {
        var diagnostics = Validate(Fixture("Valid"), app => app.MapGet("/docker-test", () => "host"));

        diagnostics.Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.RouteCollision &&
            diagnostic.Message.Contains("docker-test"));
    }

    [TestMethod]
    [DataRow("value", "value")]
    [DataRow("value", "Value")]
    public void DuplicateParameterNameIsReported(string first, string second)
    {
        ShouldReportShapeViolation(
            Manifest("dup", extras: $"""
                :parameter [ :name "{first}" ; :description "d" ; :example "x" ], [ :name "{second}" ; :description "d2" ; :example "y" ] ;
                """),
            "Parameter name must be unique within its endpoint");
    }

    [TestMethod]
    public void LiteralParameterDatatypeIsReported()
    {
        ShouldReportShapeViolation(
            Manifest("datatype", extras: """:parameter [ :name "value" ; :description "d" ; :example "x" ; :datatype "integer" ] ;"""),
            "Parameter datatype must be an IRI");
    }

    [TestMethod]
    public void MissingParameterNameIsReported()
    {
        ShouldReportShapeViolation(
            Manifest("noname", extras: """:parameter [ :description "d" ; :example "x" ] ;"""),
            "Parameter must have at least one name");
    }

    [TestMethod]
    public void InvalidParameterNameIsReported()
    {
        ShouldReportShapeViolation(
            Manifest("badname", extras: """:parameter [ :name "not valid" ; :description "d" ; :example "x" ] ;"""),
            "Parameter name must be a valid SPARQL variable name");
    }

    [TestMethod]
    public void LiteralFrameIsReported()
    {
        ShouldReportShapeViolation(
            Manifest("frameliteral", extras: """:frame "./f.jsonld" ;"""),
            "Endpoint frame must be xsd:anyURI literal");
    }

    [TestMethod]
    public void RelativeFrameWithoutDotSlashIsReported()
    {
        ShouldReportShapeViolation(
            Manifest("framed", "f.jsonld"),
            "Endpoint frame must start with './'");
    }

    [TestMethod]
    public void LeadingSlashPathIsReported()
    {
        ShouldReportShapeViolation(
            Manifest("slash", path: "/slash"),
            "Endpoint path must not start with '/'");
    }

    [TestMethod]
    public void TypedParameterHasNoDiagnostics()
    {
        using var directory = new TempDefinitions(
            Manifest("typed", extras: """:parameter [ :name "value_1" ; :description "d" ; :example "1" ; :datatype xsd:integer ] ;"""),
            "SELECT * WHERE { ?s ?p ?value_1 }");

        Validate(directory.Provider).Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("SELECT * WHERE { ?s ?p ?o }")]
    [DataRow("SELECT * WHERE { ?s ?p ?Value }")]
    [DataRow("SELECT * WHERE { ?s ?p \"?value\" } # ?value")]
    public void UnusedParameterIsReported(string sparql)
    {
        using var directory = new TempDefinitions(
            Manifest("unused", extras: """:parameter [ :name "value" ; :description "d" ; :example "x" ] ;"""),
            sparql);

        var diagnostic = Validate(directory.Provider).Should().ContainSingle().Subject;

        diagnostic.Code.Should().Be(DiagnosticCodes.ParameterUnused);
        diagnostic.Message.Should().Contain("value (q.sparql)");
        diagnostic.Line.Should().NotBeNull();
    }

    [TestMethod]
    [DataRow("SELECT * WHERE { ?s ?p ?value }")]
    [DataRow("SELECT * WHERE { ?s ?p $value }")]
    public void UsedParameterHasNoDiagnostics(string sparql)
    {
        using var directory = new TempDefinitions(
            Manifest("used", extras: """:parameter [ :name "value" ; :description "d" ; :example "x" ] ;"""),
            sparql);

        Validate(directory.Provider).Should().BeEmpty();
    }

    [TestMethod]
    public void HostPostOnlyRouteDoesNotCollide()
    {
        Validate(Fixture("Valid"), app => app.MapPost("/docker-test", () => "host")).Should().BeEmpty();
    }

    private static IFileProvider Fixture(string name) => new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private static string Manifest(string name, string? frame = null, string extras = "", string? path = null) => $$"""
        PREFIX : <http://example.org/namedquery/>
        PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

        [
            :name "{{name}}" ;
            :endpoint [
                :path "{{path ?? name}}" ;
                :sparql "./q.sparql"^^xsd:anyURI ;
                {{(frame is null ? "" : $":frame \"{frame}\"^^xsd:anyURI ;")}}
                :name "{{name}}" ;
                :description "{{name}}" ;
                {{extras}}
            ] ;
        ] .
        """;

    private static void ShouldReportShapeViolation(string manifest, string message)
    {
        using var directory = new TempDefinitions(manifest, "SELECT * WHERE { ?s ?p ?o }", "{}");

        var diagnostics = Validate(directory.Provider);

        diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.ShapeViolation &&
            diagnostic.Message.Contains(message));
        diagnostics.Should().NotContain(diagnostic => diagnostic.Code == DiagnosticCodes.Internal);
    }

    private static IReadOnlyList<Diagnostic> Validate(IFileProvider definitions, Action<WebApplication>? map = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["NamedQuery:SparqlEndpoint"] = "http://sparql.test/";
        builder.Services.AddNamedQuery(options => options.Definitions = definitions);

        var app = builder.Build();
        app.UseNamedQuery();
        map?.Invoke(app);
        app.Start();

        try
        {
            return Validator.Validate(app.Services);
        }
        finally
        {
            app.StopAsync().GetAwaiter().GetResult();
        }
    }

    private sealed class TempDefinitions : IDisposable
    {
        private readonly string path = Directory.CreateTempSubdirectory().FullName;

        internal TempDefinitions(string manifest, string? sparql = null, string? frame = null)
        {
            File.WriteAllText(Path.Combine(path, "manifest.ttl"), manifest);
            if (sparql is not null)
            {
                File.WriteAllText(Path.Combine(path, "q.sparql"), sparql);
            }

            if (frame is not null)
            {
                File.WriteAllText(Path.Combine(path, "f.jsonld"), frame);
            }

            Provider = new PhysicalFileProvider(path);
        }

        internal PhysicalFileProvider Provider { get; }

        public void Dispose()
        {
            Provider.Dispose();
            Directory.Delete(path, true);
        }
    }
}
