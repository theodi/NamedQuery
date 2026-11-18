using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Text;

namespace Test;

[TestClass]
public sealed class DecompressionTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task SparqlEndpointResponsesAreRequestedCompressedAndDecompressed()
    {
        string? acceptEncoding = null;

        // A real HTTP server, because decompression happens in the socket handler that the other tests replace with a stub.
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        await using var sparql = builder.Build();
        sparql.Map("/", async context =>
        {
            acceptEncoding = context.Request.Headers.AcceptEncoding;
            context.Response.ContentType = "application/sparql-results+json";
            context.Response.Headers.ContentEncoding = "gzip";
            await using var gzip = new GZipStream(context.Response.Body, CompressionLevel.Fastest);
            await gzip.WriteAsync(Encoding.UTF8.GetBytes("""{"head":{},"boolean":true}"""), context.RequestAborted);
        });
        await sparql.StartAsync(TestContext.CancellationToken);
        var address = sparql.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();

        var app = new MyWebApplication(static _ => { }, new Dictionary<string, string?> { ["NamedQuery:SparqlEndpoint"] = address });
        var response = await app.Client.GetAsync("endpoint1", TestContext.CancellationToken);

        response.Should().Be200Ok();
        (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Should().Contain("<boolean>true</boolean>");
        acceptEncoding.Should().Contain("gzip").And.Contain("br");
    }
}
