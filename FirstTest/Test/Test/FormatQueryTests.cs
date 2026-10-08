namespace Test;

[TestClass]
public sealed class FormatQueryTests
{
    private static readonly MyWebApplication app = new();

    public required TestContext TestContext { get; set; }

    [TestMethod]
    [DataRow("endpoint2?format=csv", "text/csv")]
    [DataRow("endpoint2?format=html", "text/html")]
    [DataRow("endpoint2?format=rdf", "application/rdf+xml")]
    [DataRow("endpoint3/something?format=html", "text/html")]
    [DataRow("endpoint4?format=ttl", "text/turtle")]
    [DataRow("endpoint4?format=nt", "application/n-triples")]
    [DataRow("endpoint4?format=jsonld", "application/ld+json")]
    [DataRow("endpoint4?format=rdf", "application/rdf+xml")]
    public async Task ServesRequestedFormat(string path, string mediaType)
    {
        var response = await app.Client.GetAsync(path, TestContext.CancellationToken);

        response.Should().Be200Ok();
        response.Content.Headers.ContentType!.MediaType.Should().Be(mediaType);
    }

    [TestMethod]
    public async Task FormatUnsuitableForResultIsNotAcceptable()
    {
        var response = await app.Client.GetAsync("endpoint2?format=ttl", TestContext.CancellationToken);

        response.Should().Be406NotAcceptable();
    }

    [TestMethod]
    [DataRow("unknown?format=ttl")]
    [DataRow("endpoint2?format=unknown")]
    public async Task UnknownIsNotFound(string path)
    {
        var response = await app.Client.GetAsync(path, TestContext.CancellationToken);

        response.Should().Be404NotFound();
    }
}
