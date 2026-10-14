using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net;

namespace Test;

[TestClass]
public sealed class McpTests
{
    private static readonly MyWebApplication app = new();

    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task ListsToolPerEndpoint()
    {
        await using var client = await Client(app);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);

        tools.Select(tool => tool.Name).Should().BeEquivalentTo("endpoint1", "endpoint2", "endpoint3_something", "endpoint4", "endpoint5", "endpoint6");
    }

    [TestMethod]
    public async Task ToolDescribesEndpoint()
    {
        await using var client = await Client(app);

        var tool = (await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).Single(tool => tool.Name == "endpoint6").ProtocolTool;

        tool.Title.Should().Be("Triples by object");
        tool.Description.Should().StartWith("Selects triples whose object is the supplied value.").And.Contain("```sparql");
        tool.Annotations!.ReadOnlyHint.Should().BeTrue();

        var schema = tool.InputSchema;
        schema.GetProperty("required")[0].GetString().Should().Be("value");
        var value = schema.GetProperty("properties").GetProperty("value");
        value.GetProperty("type").GetString().Should().Be("string");
        value.GetProperty("description").GetString().Should().Be("Literal object to match.");
        value.GetProperty("examples")[0].GetString().Should().Be("example");
    }

    [TestMethod]
    [DataRow("endpoint2", "True")]
    [DataRow("endpoint3_something", "True")]
    [DataRow("endpoint4", "<urn:example:s>")]
    public async Task CallingToolReturnsResult(string name, string expected)
    {
        await using var client = await Client(app);

        var result = await client.CallToolAsync(name, cancellationToken: TestContext.CancellationToken);

        result.IsError.Should().NotBeTrue();
        result.Content.OfType<TextContentBlock>().Single().Text.Should().Contain(expected);
    }

    [TestMethod]
    public async Task CallingToolWithArgumentReturnsResult()
    {
        await using var client = await Client(app);

        var result = await client.CallToolAsync("endpoint6", new Dictionary<string, object?> { ["value"] = "x" }, cancellationToken: TestContext.CancellationToken);

        result.IsError.Should().NotBeTrue();
    }

    [TestMethod]
    public async Task MissingArgumentIsToolError()
    {
        await using var client = await Client(app);

        var result = await client.CallToolAsync("endpoint6", cancellationToken: TestContext.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.OfType<TextContentBlock>().Single().Text.Should().Be("Missing required arguments: value");
    }

    [TestMethod]
    public async Task UnknownToolIsProtocolError()
    {
        await using var client = await Client(app);

        var call = () => client.CallToolAsync("unknown", cancellationToken: TestContext.CancellationToken).AsTask();

        (await call.Should().ThrowAsync<McpProtocolException>()).Which.ErrorCode.Should().Be(McpErrorCode.InvalidParams);
    }

    [TestMethod]
    [DataRow("endpoint2")]
    [DataRow("endpoint4")]
    public async Task UpstreamFailureIsToolError(string name)
    {
        var failing = new MyWebApplication(client => client.ConfigurePrimaryHttpMessageHandler(() => new FailingSparqlStub(HttpStatusCode.InternalServerError)));
        await using var client = await Client(failing);

        var result = await client.CallToolAsync(name, cancellationToken: TestContext.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.OfType<TextContentBlock>().Single().Text.Should().Be("SPARQL endpoint failed: Server reports 500: Internal Server Error.");
    }

    [TestMethod]
    [DataRow("endpoint2")]
    [DataRow("endpoint4")]
    public async Task UpstreamTimeoutIsToolError(string name)
    {
        var hanging = new MyWebApplication(client => client
            .ConfigurePrimaryHttpMessageHandler(() => new HangingSparqlStub())
            .ConfigureHttpClient(http => http.Timeout = TimeSpan.FromMilliseconds(100)));
        await using var client = await Client(hanging);

        var result = await client.CallToolAsync(name, cancellationToken: TestContext.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.OfType<TextContentBlock>().Single().Text.Should().StartWith("SPARQL endpoint timed out:").And.Contain("HttpClient.Timeout");
    }

    private async Task<McpClient> Client(MyWebApplication application) => await McpClient.CreateAsync(
        new HttpClientTransport(new() { Endpoint = new(application.Client.BaseAddress!, "mcp"), TransportMode = HttpTransportMode.StreamableHttp }, application.Client),
        cancellationToken: TestContext.CancellationToken);
}
