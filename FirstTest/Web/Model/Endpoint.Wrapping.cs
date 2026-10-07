using VDS.RDF;
using VDS.RDF.Wrapping;

namespace Web.Model;

public partial class Endpoint : GraphWrapperNode
{
    protected Endpoint(INode node, IGraph graph) : base(node, graph) { }

    public string? Path => this.Singular(Vocabulary.Path, ValueMappings.As<string>);

    public Uri? Sparql => this.Singular(Vocabulary.Sparql, ValueMappings.As<Uri>);

    public static Endpoint Wrap(GraphWrapperNode node) => new(node, node.Graph);
}
