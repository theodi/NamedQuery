using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Test;

[TestClass]
public sealed class UpstreamErrorTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    [DataRow(HttpStatusCode.InternalServerError, "endpoint2", "Server reports 500: Internal Server Error.")]
    [DataRow(HttpStatusCode.InternalServerError, "endpoint4", "Server reports 500: Internal Server Error.")]
    [DataRow(HttpStatusCode.ServiceUnavailable, "endpoint2", "Server reports 503: Service Unavailable.")]
    [DataRow(null, "endpoint2", "Connection refused ---> Socket closed")]
    [DataRow(null, "endpoint4", "Connection refused ---> Socket closed")]
    public async Task UpstreamFailureIsBadGateway(HttpStatusCode? status, string path, string detail)
    {
        var app = new MyWebApplication(client => client.ConfigurePrimaryHttpMessageHandler(() => new FailingSparqlStub(status)));
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd("text/turtle");

        var response = await app.Client.SendAsync(request, TestContext.CancellationToken);

        response.Should().Be502BadGateway();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(502);
        problem.RootElement.GetProperty("detail").GetString().Should().Be(detail);
    }

    [TestMethod]
    [DataRow("endpoint2")]
    [DataRow("endpoint4")]
    public async Task UpstreamTimeoutIsGatewayTimeout(string path)
    {
        var app = new MyWebApplication(client => client
            .ConfigurePrimaryHttpMessageHandler(() => new HangingSparqlStub())
            .ConfigureHttpClient(http => http.Timeout = TimeSpan.FromMilliseconds(100)));
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd("text/turtle");

        var response = await app.Client.SendAsync(request, TestContext.CancellationToken);

        response.Should().Be504GatewayTimeout();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(504);
        problem.RootElement.GetProperty("detail").GetString().Should().Contain("HttpClient.Timeout");
    }
}
