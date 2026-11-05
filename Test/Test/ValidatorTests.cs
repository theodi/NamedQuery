using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
            diagnostic.Message.Contains("Endpoint path collides with route: Index"));
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
    public void ReservedParameterIsReported()
    {
        Validate(Fixture("Reserved")).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.FormatParameterCollision &&
            diagnostic.Message.Contains("format (reserved)"));
    }

    [TestMethod]
    public void ToolNameCollisionIsReported()
    {
        Validate(Fixture("ToolName")).Should().ContainSingle().Which.Should().Match<Diagnostic>(diagnostic =>
            diagnostic.Code == DiagnosticCodes.ToolNameCollision &&
            diagnostic.Message.Contains("tool_name (tool/name, tool_name)"));
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
    public void HostPostOnlyRouteDoesNotCollide()
    {
        Validate(Fixture("Valid"), app => app.MapPost("/docker-test", () => "host")).Should().BeEmpty();
    }

    private static IFileProvider Fixture(string name) => new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private static string Manifest(string name, string? frame = null) => $$"""
        PREFIX : <http://example.org/namedquery/>
        PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

        [
            :name "{{name}}" ;
            :endpoint [
                :path "{{name}}" ;
                :sparql "./q.sparql"^^xsd:anyURI ;
                {{(frame is null ? "" : $":frame \"{frame}\"^^xsd:anyURI ;")}}
                :name "{{name}}" ;
                :description "{{name}}" ;
            ] ;
        ] .
        """;

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
