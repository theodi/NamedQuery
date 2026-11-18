using Microsoft.Net.Http.Headers;
using VDS.RDF;

namespace Test;

[TestClass]
public sealed class ContentNegotiationTests
{
    private static readonly MyWebApplication app = new();

    public static IEnumerable<object[]> ResultsMediaTypes => MediaTypes(definition => definition.CanWriteSparqlResults && !MimeTypesHelper.RdfXml.Contains(definition.CanonicalMimeType));

    public static IEnumerable<object[]> GraphMediaTypes => MediaTypes(definition => definition.CanWriteRdf || definition.CanWriteRdfDatasets);

    public required TestContext TestContext { get; set; }

    [TestMethod]
    [DynamicData(nameof(ResultsMediaTypes))]
    public async Task ResultsInEveryFormat(string mediaType) => await ServesAs("endpoint2", mediaType);

    [TestMethod]
    [DynamicData(nameof(GraphMediaTypes))]
    public async Task GraphInEveryFormat(string mediaType) => await ServesAs("endpoint4", mediaType);

    [TestMethod]
    [DataRow("endpoint2", "application/sparql-results+xml")]
    [DataRow("endpoint4", "application/ld+json")]
    public async Task DefaultsWithoutAccept(string path, string mediaType)
    {
        var response = await app.Client.GetAsync(path, TestContext.CancellationToken);

        response.Should().Be200Ok();
        response.Content.Headers.ContentType!.MediaType.Should().Be(mediaType);
    }

    [TestMethod]
    [DataRow("endpoint2?format=csv", "application/sparql-results+xml")]
    [DataRow("endpoint4?format=ttl", "application/ld+json")]
    public async Task IgnoresFormatQueryParameter(string path, string mediaType)
    {
        var response = await app.Client.GetAsync(path, TestContext.CancellationToken);

        response.Should().Be200Ok();
        response.Content.Headers.ContentType!.MediaType.Should().Be(mediaType);
    }

    [TestMethod]
    public async Task FramedJsonLd()=> await ServesAs("endpoint5", "application/ld+json");

    [TestMethod]
    [DataRow("endpoint2", "image/png")]
    [DataRow("endpoint1", "application/rdf+xml")]
    [DataRow("endpoint2", "application/rdf+xml")]
    [DataRow("endpoint1", "application/ld+json")]
    [DataRow("endpoint1", "application/vnd.example+json")]
    [DataRow("endpoint2", "application/vnd.example+json")]
    public async Task UnsupportedFormatIsNotAcceptable(string path, string mediaType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd(mediaType);

        var response = await app.Client.SendAsync(request, TestContext.CancellationToken);

        response.Should().Be406NotAcceptable();
    }

    private static IEnumerable<object[]> MediaTypes(Func<MimeTypeDefinition, bool> canWrite) =>
        MimeTypesHelper.Definitions.Where(canWrite).Select(definition => definition.CanonicalMimeType).Distinct().Select(mediaType => new object[] { mediaType });

    private async Task ServesAs(string path, string mediaType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd(mediaType);

        var response = await app.Client.SendAsync(request, TestContext.CancellationToken);

        response.Should().Be200Ok();
        MediaTypeHeaderValue.Parse(response.Content.Headers.ContentType!.MediaType).IsSubsetOf(MediaTypeHeaderValue.Parse(mediaType)).Should().BeTrue();
        (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Should().NotBeEmpty();
    }
}
