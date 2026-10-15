using VDS.RDF;
using VDS.RDF.Wrapping;

namespace TheODI.NamedQuery.Model;

internal partial class ManifestNode : GraphWrapperNode
{
    protected ManifestNode(INode node, IGraph graph) : base(node, graph) { }

    public string Name => this.Singular(Vocabulary.Name, ValueMappings.As<string>, throwWhenMissing: true)!;

    public ISet<Endpoint> Endpoint => this.Objects(Vocabulary.Endpoint, Model.Endpoint.Wrap, Model.Endpoint.Wrap);

    public static ManifestNode Wrap(GraphWrapperNode node) => new(node, node.Graph);
}
