using VDS.RDF;
using VDS.RDF.Wrapping;

namespace Web.Model;

public partial class Parameter : GraphWrapperNode
{
    protected Parameter(INode node, IGraph graph) : base(node, graph) { }

    public string Name => this.Singular(Vocabulary.Name, ValueMappings.As<string>, throwWhenMissing: true);

    private Uri? DatatypeInternal => this.Singular(Vocabulary.Datatype, ValueMappings.As<Uri>);

    public static Parameter Wrap(INode node, IGraph graph) => new(node, graph);

    public static Parameter Wrap(GraphWrapperNode node) => Wrap(node, node.Graph);
}
