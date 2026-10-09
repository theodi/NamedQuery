using System.Text.Json;

namespace Test;

[TestClass]
public sealed class ParameterTests
{
    private static readonly MyWebApplication app = new();

    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task SuppliedParameterIsServed()
    {
        var response = await app.Client.GetAsync("endpoint6?value=x", TestContext.CancellationToken);

        response.Should().Be200Ok();
    }

    [TestMethod]
    [DataRow("endpoint6", null)]
    [DataRow("endpoint6?format=html", null)]
    [DataRow("endpoint6?format=ttl", null)]
    [DataRow("endpoint6?other=x", null)]
    [DataRow("endpoint6", "text/turtle")]
    [DataRow("endpoint6", "application/sparql-results+json")]
    [DataRow("endpoint6", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    public async Task MissingParameterIsValidationProblem(string path, string? accept)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (accept is not null)
        {
            request.Headers.Accept.ParseAdd(accept);
        }

        var response = await app.Client.SendAsync(request, TestContext.CancellationToken);

        response.Should().Be400BadRequest();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        problem.RootElement.GetProperty("errors").GetProperty("value")[0].GetString().Should().Be("This query string parameter is required");
    }
}
