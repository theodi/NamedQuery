namespace Test;

[TestClass]
public sealed class SwaggerUiTests
{
    private static readonly MyWebApplication app = new();

    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task ServesUi()
    {
        var response = await app.Client.GetAsync("swagger/index.html", TestContext.CancellationToken);

        response.Should().Be200Ok();
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        html.Should().Contain("swagger-ui");
    }

    [TestMethod]
    public async Task PointsAtOpenApiDocument()
    {
        var script = await app.Client.GetStringAsync("swagger/index.js", TestContext.CancellationToken);

        script.Should().Contain("\"url\":\"/openapi.json\"");
    }
}
