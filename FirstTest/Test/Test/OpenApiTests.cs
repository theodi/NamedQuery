using System.Text.Json.Nodes;

namespace Test;

[TestClass]
public sealed class OpenApiTests
{
    private static readonly MyWebApplication app = new();

    public required TestContext TestContext { get; set; }

    [TestMethod]
    [DataRow("openapi.json", "application/openapi+json; version=3.2; charset=utf-8")]
    [DataRow("openapi.yaml", "application/openapi+yaml; version=3.2; charset=utf-8")]
    public async Task ServesFormatByExtension(string path, string mediaType)
    {
        var response = await app.Client.GetAsync(path, TestContext.CancellationToken);

        response.Should().Be200Ok();
        response.Content.Headers.ContentType!.ToString().Should().Be(mediaType);
    }

    [TestMethod]
    [DataRow("openapi.json", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    [DataRow("openapi.json", "application/json")]
    [DataRow("openapi.yaml", "application/yaml")]
    public async Task AcceptsGenericMediaTypes(string path, string accept)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd(accept);

        var response = await app.Client.SendAsync(request, TestContext.CancellationToken);

        response.Should().Be200Ok();
    }

    [TestMethod]
    public async Task ServesYaml()
    {
        var yaml = await app.Client.GetStringAsync("openapi.yaml", TestContext.CancellationToken);

        yaml.Should().StartWith("openapi: '3.2").And.Contain("/endpoint6:").And.Contain("$ref: '#/components/responses/results'");
    }

    [TestMethod]
    public async Task UnsupportedExtensionIsNotFound()
    {
        var response = await app.Client.GetAsync("openapi.ttl", TestContext.CancellationToken);

        response.Should().Be404NotFound();
    }

    [TestMethod]
    public async Task DeclaresOpenApi32()
    {
        var document = await Document();

        document["openapi"]!.GetValue<string>().Should().StartWith("3.2");
    }

    [TestMethod]
    public async Task DescribesEachEndpoint()
    {
        var document = await Document();

        document["paths"]!.AsObject().Select(path => path.Key).Should().BeEquivalentTo("/endpoint1", "/endpoint2", "/endpoint3/something", "/endpoint4", "/endpoint5", "/endpoint6");
    }

    [TestMethod]
    public async Task DescribesOperation()
    {
        var document = await Document();

        var operation = document["paths"]!["/endpoint6"]!["get"]!;
        operation["summary"]!.GetValue<string>().Should().Be("Triples by object");
        operation["description"]!.GetValue<string>().Should().StartWith("Selects triples whose object is the supplied value.").And.Contain("BIND($value AS ?o)");
    }

    [TestMethod]
    public async Task DescribesParameters()
    {
        var document = await Document();

        var parameter = document["paths"]!["/endpoint6"]!["get"]!["parameters"]!.AsArray().Should().ContainSingle().Subject!;
        parameter["name"]!.GetValue<string>().Should().Be("value");
        parameter["in"]!.GetValue<string>().Should().Be("query");
        parameter["required"]!.GetValue<bool>().Should().BeTrue();
        parameter["description"]!.GetValue<string>().Should().Be("Literal object to match.");
        parameter["example"]!.GetValue<string>().Should().Be("example");
        parameter["schema"]!["type"]!.GetValue<string>().Should().Be("string");
    }

    [TestMethod]
    [DataRow("/endpoint1", "results")]
    [DataRow("/endpoint2", "results")]
    [DataRow("/endpoint4", "graph")]
    [DataRow("/endpoint5", "graph")]
    public async Task ReferencesResponseByQueryType(string path, string response)
    {
        var document = await Document();

        document["paths"]![path]!["get"]!["responses"]!["200"]!["$ref"]!.GetValue<string>().Should().Be($"#/components/responses/{response}");
    }

    [TestMethod]
    public async Task ReferencesProblemResponseOnlyWithParameters()
    {
        var document = await Document();

        document["paths"]!["/endpoint6"]!["get"]!["responses"]!["400"]!["$ref"]!.GetValue<string>().Should().Be("#/components/responses/problem");
        document["paths"]!["/endpoint1"]!["get"]!["responses"]!.AsObject().ContainsKey("400").Should().BeFalse();
    }

    [TestMethod]
    [DataRow("graph", "Graph")]
    [DataRow("results", "SPARQL results")]
    [DataRow("problem", "Invalid parameter")]
    public async Task SummarisesResponses(string response, string summary)
    {
        var document = await Document();

        document["components"]!["responses"]![response]!["summary"]!.GetValue<string>().Should().Be(summary);
        document["components"]!["responses"]![response]!["description"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    [DataRow("graph", "text/turtle")]
    [DataRow("graph", "application/ld+json")]
    [DataRow("results", "application/sparql-results+xml")]
    [DataRow("results", "application/sparql-results+json")]
    [DataRow("problem", "application/problem+json")]
    public async Task DeclaresResponseMediaTypes(string response, string mediaType)
    {
        var document = await Document();

        document["components"]!["responses"]![response]!["content"]!.AsObject().ContainsKey(mediaType).Should().BeTrue();
    }

    private async Task<JsonNode> Document()
    {
        var json = await app.Client.GetStringAsync("openapi.json", TestContext.CancellationToken);
        return JsonNode.Parse(json)!;
    }
}
