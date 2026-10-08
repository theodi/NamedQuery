using Newtonsoft.Json.Linq;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace Web.Model;

public partial class Endpoint
{
    private static readonly Uri FakeBase = new("http://resources/");

    public string SparqlResourceName => new Uri(FakeBase, Sparql).GetComponents(UriComponents.Path, UriFormat.Unescaped);

    public string? FrameResourceName => Frame switch
    {
        null => null,
        var frame => new Uri(FakeBase, frame).GetComponents(UriComponents.Path, UriFormat.Unescaped)
    };

    public string Query => Resources.String(SparqlResourceName) ?? throw new Exception($"SPARQL file not found: {SparqlResourceName}");

    public JToken? JsonLdFrame => FrameResourceName switch
    {
        null => null,
        var name => Resources.Token(name)
    };

    public SparqlQueryType QueryType => new SparqlQueryParser().ParseFromString(Query).QueryType;
}
