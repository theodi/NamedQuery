using VDS.RDF;
using VDS.RDF.Wrapping;

namespace Web.Model;

public partial class Manifest : WrapperGraph
{
    protected Manifest(IGraph original) : base(original) { }

    public ManifestNode Node => this.SubjectsOf(Vocabulary.Endpoint, ManifestNode.Wrap).Single();

    public static Manifest Wrap(IGraph graph) => new(graph);
}
