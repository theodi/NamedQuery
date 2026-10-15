using VDS.RDF;
using VDS.RDF.Wrapping;

namespace TheODI.NamedQuery.Model;

internal partial class Parameter : GraphWrapperNode
{
    protected Parameter(INode node, IGraph graph) : base(node, graph) { }

    public string Name => this.Singular(Vocabulary.Name, ValueMappings.As<string>, throwWhenMissing: true)!;

    public string Description => this.Singular(Vocabulary.Description, ValueMappings.As<string>, throwWhenMissing: true)!;

    public string Example => this.Singular(Vocabulary.Example, ValueMappings.As<string>, throwWhenMissing: true)!;

    private Uri? DatatypeInternal => this.Singular(Vocabulary.Datatype, ValueMappings.As<Uri>);

    public static Parameter Wrap(INode node, IGraph graph) => new(node, graph);

    public static Parameter Wrap(GraphWrapperNode node) => Wrap(node, node.Graph);
}
