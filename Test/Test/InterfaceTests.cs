using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Test;

[TestClass]
public sealed class InterfaceTests
{
    private static readonly MyWebApplication restOnly = new(configuration: new Dictionary<string, string?> { ["NamedQuery:Mcp"] = "false" });
    private static readonly MyWebApplication mcpOnly = new(configuration: new Dictionary<string, string?> { ["NamedQuery:Rest"] = "false" });
    private static readonly MyWebApplication mcpAtRoot = new(configuration: new Dictionary<string, string?> { ["NamedQuery:Rest"] = "false", ["NamedQuery:McpPath"] = "/" });
    private static readonly MyWebApplication mcpElsewhere = new(configuration: new Dictionary<string, string?> { ["NamedQuery:McpPath"] = "/tools/mcp" });

    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task RestOnlyServesEndpoints()
    {
        var response = await restOnly.Client.GetAsync("endpoint1", TestContext.CancellationToken);

        response.Should().Be200Ok();
    }

    [TestMethod]
    public async Task RestOnlyServesOpenApi()
    {
        var response = await restOnly.Client.GetAsync("/", TestContext.CancellationToken);

        response.Should().Be200Ok();
    }

    [TestMethod]
    public async Task RestOnlyDoesNotServeMcp()
    {
        var response = await restOnly.Client.GetAsync("mcp",TestContext.CancellationToken);

        response.Should().Be404NotFound();
    }

    [TestMethod]
    public async Task McpOnlyServesMcp()
    {
        await using var client = await Client(mcpOnly, "mcp");

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);

        tools.Should().NotBeEmpty();
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("endpoint1")]
    public async Task McpOnlyDoesNotServeRest(string path)
    {
        var response = await mcpOnly.Client.GetAsync(path, TestContext.CancellationToken);

        response.Should().Be404NotFound();
    }

    [TestMethod]
    public void TurningBothOffFailsStartup()
    {
        var start = () => new MyWebApplication(configuration: new Dictionary<string, string?> { ["NamedQuery:Rest"] = "false", ["NamedQuery:Mcp"] = "false" });

        start.Should().Throw<OptionsValidationException>().WithMessage("*At least one of Rest and Mcp must be true*");
    }

    [TestMethod]
    public async Task McpCanBeServedAtRootWhenRestIsOff()
    {
        await using var client = await Client(mcpAtRoot, "");

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);

        tools.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task McpCanBeServedAtAnotherPath()
    {
        await using var client = await Client(mcpElsewhere, "tools/mcp");

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);

        tools.Should().NotBeEmpty();
        (await mcpElsewhere.Client.GetAsync("/", TestContext.CancellationToken)).Should().Be200Ok();
        (await mcpElsewhere.Client.GetAsync("mcp", TestContext.CancellationToken)).Should().Be404NotFound();
    }

    [TestMethod]
    [DataRow("mcp", "must start with '/'")]
    [DataRow("/{name}", "without route parameters")]
    [DataRow("/a{", "without route parameters")]
    [DataRow("/", "can be '/' only when Rest is false")]
    public void InvalidMcpPathFailsStartup(string path, string message)
    {
        var start = () => new MyWebApplication(configuration: new Dictionary<string, string?> { ["NamedQuery:McpPath"] = path });

        start.Should().Throw<OptionsValidationException>().WithMessage($"*{message}*");
    }

    private async Task<McpClient> Client(MyWebApplication application, string path) => await McpClient.CreateAsync(
        new HttpClientTransport(new() { Endpoint = new(application.Client.BaseAddress!, path), TransportMode = HttpTransportMode.StreamableHttp }, application.Client),
        cancellationToken: TestContext.CancellationToken);
}
