using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace TheODI.NamedQuery;

internal class Resources(IOptions<NamedQueryOptions> options)
{
    private const string ManifestName = "manifest.ttl";

    private Cache? cache;

    // The parsed manifest, queries and query types, kept until the definitions change. Failures are not cached, so they are retried on the next access.
    public Model.Manifest Manifest => Current.Manifest ??= LoadManifest();

    // Starts a new cache when the definitions change. The change token is obtained before anything is read, so a change made while reading still invalidates what was read. Providers that can't change, such as ManifestEmbeddedFileProvider, return a token that never does.
    private Cache Current
    {
        get
        {
            var current = cache;
            if (current is not null && !current.Token.HasChanged)
            {
                return current;
            }

            var fresh = new Cache(options.Value.Definitions.Watch("**/*"));
            return Interlocked.CompareExchange(ref cache, fresh, current) == current ? fresh : cache!;
        }
    }

    public IFileInfo? Info(string name) => options.Value.Definitions.GetFileInfo(name) is { Exists: true } file ? file : null;

    public StreamReader? Reader(string name) => Stream(name) is { } stream ? new StreamReader(stream) : null;

    public string Query(Model.Endpoint endpoint) => Current.Queries.GetOrAdd(endpoint.SparqlResourceName, name => String(name) ?? throw new Exception($"SPARQL file not found: {name}"));

    public JToken? JsonLdFrame(Model.Endpoint endpoint) => endpoint.FrameResourceName is { } name ? Token(name) : null;

    public SparqlQueryType QueryType(Model.Endpoint endpoint) => Current.QueryTypes.GetOrAdd(endpoint.SparqlResourceName, _ => new SparqlQueryParser().ParseFromString(Query(endpoint)).QueryType);

    private Model.Manifest LoadManifest()
    {
        using var reader = Reader(ManifestName) ?? throw new Exception("Manifest not found");
        var graph = new Graph();
        new TurtleParser().Load(graph, reader);
        return Model.Manifest.Wrap(graph);
    }

    private string? String(string name)
    {
        using var reader = Reader(name);
        return reader?.ReadToEnd();
    }

    private JToken? Token(string name) => Reader(name) is { } reader ? JToken.ReadFrom(new JsonTextReader(reader)) : null;

    private Stream? Stream(string name) => Info(name)?.CreateReadStream();

    private sealed class Cache(IChangeToken token)
    {
        internal IChangeToken Token { get; } = token;

        internal Model.Manifest? Manifest { get; set; }

        internal ConcurrentDictionary<string, string> Queries { get; } = new();

        internal ConcurrentDictionary<string, SparqlQueryType> QueryTypes { get; } = new();
    }
}
