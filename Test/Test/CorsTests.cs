namespace Test;

[TestClass]
public sealed class CorsTests
{
    private static readonly MyWebApplication app = new();

    public required TestContext TestContext { get; set; }

    [TestMethod]
    [DataRow("")]
    [DataRow("endpoint1")]
    public async Task AllowsAnyOrigin(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Origin", "http://example.com");

        var response = await app.Client.SendAsync(request, TestContext.CancellationToken);

        response.Should().HaveHeader("Access-Control-Allow-Origin").And.Match("*");
    }
}
