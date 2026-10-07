namespace Test;

[TestClass]
public sealed class DefaultControllerTests
{
    private static readonly MyWebApplication app = new();

    public required TestContext TestContext { get; set; }

    [TestMethod]
    [DataRow("endpoint1")]
    [DataRow("endpoint2")]
    [DataRow("endpoint3/something")]
    public async Task ServesEndpoint(string path)
    {
        var response = await app.Client.GetAsync(path, TestContext.CancellationToken);

        response.Should().Be200Ok();
        var results = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        results.Should().MatchRegex("\"boolean\"\\s*:\\s*true");
    }

    [TestMethod]
    public async Task UnknownEndpointIsNotFound()
    {
        var response = await app.Client.GetAsync("unknown", TestContext.CancellationToken);

        response.Should().Be404NotFound();
    }
}
