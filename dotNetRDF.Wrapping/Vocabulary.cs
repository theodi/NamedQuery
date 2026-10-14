using VDS.RDF;
using VDS.RDF.Parsing;

namespace VDS.RDF.Wrapping;

internal class Vocabulary
{
    private static readonly NodeFactory Factory = new();


    private static IUriNode Node(string name) => Factory.CreateUriNode(UriFactory.Create(name));

    internal static IUriNode RdfType { get; } = Node(RdfSpecsHelper.RdfType);

    public static INode Nil { get; } = Node(RdfSpecsHelper.RdfListNil);
}
