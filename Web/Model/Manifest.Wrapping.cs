using VDS.RDF;
using VDS.RDF.Wrapping;

namespace Web.Model;

public partial class Manifest : WrapperGraph
{
    protected Manifest(IGraph original) : base(original) { }

    public IEnumerable<Endpoint> Endpoints => this.SubjectsOf(Vocabulary.Path, Endpoint.Wrap);

    public static Manifest Wrap(IGraph graph) => new(graph);
}
