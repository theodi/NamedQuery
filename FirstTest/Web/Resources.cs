using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace Web;

public class Resources(IOptions<Model.Options> options)
{
    private const string ManifestName = "manifest.ttl";

    public Model.Manifest Manifest
    {
        get
        {
            using var reader = Reader(ManifestName) ?? throw new Exception("Manifest not found");
            var graph = new Graph();
            new TurtleParser().Load(graph, reader);
            return Model.Manifest.Wrap(graph);
        }
    }

    public IFileInfo? Info(string name) => options.Value.Definitions.GetFileInfo(name) is { Exists: true } file ? file : null;

    public StreamReader? Reader(string name) => Stream(name) is { } stream ? new StreamReader(stream) : null;

    public string Query(Model.Endpoint endpoint) => String(endpoint.SparqlResourceName) ?? throw new Exception($"SPARQL file not found: {endpoint.SparqlResourceName}");

    public JToken? JsonLdFrame(Model.Endpoint endpoint) => endpoint.FrameResourceName is { } name ? Token(name) : null;

    public SparqlQueryType QueryType(Model.Endpoint endpoint) => new SparqlQueryParser().ParseFromString(Query(endpoint)).QueryType;

    private string? String(string name)
    {
        using var reader = Reader(name);
        return reader?.ReadToEnd();
    }

    private JToken? Token(string name) => Reader(name) is { } reader ? JToken.ReadFrom(new JsonTextReader(reader)) : null;

    private Stream? Stream(string name) => Info(name)?.CreateReadStream();
}
