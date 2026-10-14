using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;
using Web.Controllers;

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

    [TestMethod]
    public async Task ShowsEndpointNameAndDescription()
    {
        var html = await app.Client.GetStringAsync("", TestContext.CancellationToken);

        html.Should().Contain("<a href=\"endpoint1\">Any ten triples</a> (Selects up to ten arbitrary triples.)");
    }

    [TestMethod]
    public async Task LinksToEndpointWithExampleParameters()
    {
        var html = await app.Client.GetStringAsync("", TestContext.CancellationToken);
        html.Should().Contain("href=\"endpoint6?value=example\"");

        var response = await app.Client.GetAsync("endpoint6?value=example", TestContext.CancellationToken);
        response.Should().Be200Ok();
    }

    [TestMethod]
    public async Task LinksToSwaggerUi()
    {
        var href = $"/{app.Services.GetRequiredService<IOptions<SwaggerUIOptions>>().Value.RoutePrefix}";

        var html = await app.Client.GetStringAsync("", TestContext.CancellationToken);
        html.Should().Contain($"href=\"{href}\"");

        var response = await app.Client.GetAsync(href, TestContext.CancellationToken);
        response.Should().Be200Ok();
        var ui = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        ui.Should().Contain("swagger-ui");
    }

    [TestMethod]
    [DataRow(nameof(OpenApiController.GetJson), "application/openapi+json")]
    [DataRow(nameof(OpenApiController.GetYaml), "application/openapi+yaml")]
    public async Task LinksToOpenApiDocument(string routeName, string mediaType)
    {
        var href = app.Services.GetRequiredService<LinkGenerator>().GetPathByName(routeName, null);

        var html = await app.Client.GetStringAsync("", TestContext.CancellationToken);
        html.Should().Contain($"href=\"{href}\"");

        var response = await app.Client.GetAsync(href, TestContext.CancellationToken);
        response.Should().Be200Ok();
        response.Content.Headers.ContentType!.MediaType.Should().Be(mediaType);
    }
}
