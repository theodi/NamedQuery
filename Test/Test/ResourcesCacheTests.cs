using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TheODI.NamedQuery;
using VDS.RDF.Query;

namespace Test;

[TestClass]
public sealed class ResourcesCacheTests
{
    [TestMethod]
    public void DefinitionsAreReadOnceWhileUnchanged()
    {
        var definitions = new CountingFileProvider();
        var resources = Resources(definitions);

        for (var i = 0; i < 3; i++)
        {
            var endpoint = resources.Manifest["endpoint1"]!;
            resources.Query(endpoint);
            resources.QueryType(endpoint);
        }

        definitions.Reads.Should().BeEquivalentTo(["manifest.ttl", "endpoint1.sparql"]);
    }

    [TestMethod]
    public void DefinitionsAreReadAgainAfterChange()
    {
        var definitions = new CountingFileProvider();
        var resources = Resources(definitions);
        var before = resources.Manifest;
        resources.QueryType(before["endpoint1"]!).Should().Be(SparqlQueryType.SelectAll);

        definitions.Change();
        var after = resources.Manifest;
        resources.QueryType(after["endpoint1"]!);

        after.Should().NotBeSameAs(before);
        definitions.Reads.Should().BeEquivalentTo(["manifest.ttl", "endpoint1.sparql", "manifest.ttl", "endpoint1.sparql"]);
    }

    [TestMethod]
    public void FailuresAreNotCached()
    {
        var definitions = new CountingFileProvider { Missing = "manifest.ttl" };
        var resources = Resources(definitions);

        var read = () => resources.Manifest;
        read.Should().Throw<Exception>().WithMessage("Manifest not found");

        definitions.Missing = null;
        read.Should().NotThrow();
    }

    private static Resources Resources(IFileProvider definitions) => new(Options.Create(new NamedQueryOptions { SparqlEndpoint = new("http://sparql.test/"), Definitions = definitions }));

    // The test endpoint definitions, recording which files are read and with a change token the test triggers.
    private sealed class CountingFileProvider : IFileProvider
    {
        private readonly ManifestEmbeddedFileProvider inner = new(typeof(CountingFileProvider).Assembly);
        private CancellationTokenSource changes = new();

        internal List<string> Reads { get; } = [];

        internal string? Missing { get; set; }

        internal void Change()
        {
            var old = changes;
            changes = new();
            old.Cancel();
        }

        public IFileInfo GetFileInfo(string subpath) => subpath == Missing
            ? new NotFoundFileInfo(subpath)
            : new CountingFileInfo(inner.GetFileInfo(subpath), () => Reads.Add(subpath));

        public IDirectoryContents GetDirectoryContents(string subpath) => inner.GetDirectoryContents(subpath);

        public IChangeToken Watch(string filter) => new CancellationChangeToken(changes.Token);
    }

    private sealed class CountingFileInfo(IFileInfo inner, Action onRead) : IFileInfo
    {
        public bool Exists => inner.Exists;

        public long Length => inner.Length;

        public string? PhysicalPath => inner.PhysicalPath;

        public string Name => inner.Name;

        public DateTimeOffset LastModified => inner.LastModified;

        public bool IsDirectory => inner.IsDirectory;

        public Stream CreateReadStream()
        {
            onRead();
            return inner.CreateReadStream();
        }
    }
}
