using VDS.RDF;

namespace Web.Model;

internal static class Vocabulary
{
    private const string Base = "http://example.org/namedquery/";
    private static readonly NodeFactory Factory = new();

    internal static IUriNode Path { get; } = Node("path");

    internal static IUriNode Sparql { get; } = Node("sparql");

    internal static IUriNode Frame { get; } = Node("frame");

    internal static IUriNode Parameter { get; } = Node("parameter");

    internal static IUriNode Name { get; } = Node("name");

    internal static IUriNode Datatype { get; } = Node("datatype");

    private static IUriNode Node(string name) => Factory.CreateUriNode(UriFactory.Create($"{Base}{name}"));
}
