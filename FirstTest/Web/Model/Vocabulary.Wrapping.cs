using VDS.RDF;

namespace Web.Model;

internal static class Vocabulary
{
    private const string Base = "http://example.org/namedquery/";
    private static readonly NodeFactory Factory = new();

    internal static IUriNode Path { get; } = Node("path");

    internal static IUriNode Sparql { get; } = Node("sparql");

    internal static IUriNode Frame { get; } = Node("frame");

    private static IUriNode Node(string name) => Factory.CreateUriNode(UriFactory.Create($"{Base}{name}"));
}
