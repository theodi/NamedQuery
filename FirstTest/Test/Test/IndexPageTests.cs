namespace Test;

[TestClass]
public sealed class IndexPageTests
{
    private static readonly MyWebApplication app = new();

    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task LinksToEachEndpoint()
    {
        var response = await app.Client.GetAsync("", TestContext.CancellationToken);

        response.Should().Be200Ok();
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        html.Should().Contain("href=\"endpoint1\"").And.Contain("href=\"endpoint2\"").And.Contain("href=\"endpoint3/something\"");
    }
}
