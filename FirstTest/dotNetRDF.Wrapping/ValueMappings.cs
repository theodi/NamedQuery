using System.Globalization;
using System.Text.RegularExpressions;
using VDS.RDF.Nodes;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Wrapping;

public static class ValueMappings
{
    private const string hash = "#";

    public static T? As<T>(GraphWrapperNode? node) =>
        node!.AsObject() switch
        {
            null => default,
            T typed => typed,
            var result => throw new InvalidCastException($"Cannot cast [{result}] to [{typeof(T)}]"),
        };

    public static string StringFromIri(GraphWrapperNode? node) => (node as IUriNode).Uri.ToString();

    public static GraphWrapperNode? AsIs(GraphWrapperNode? node) => node;

    public static KeyValuePair<CultureInfo, string> AsLangStringPair(GraphWrapperNode? node) => new(
        CultureInfo.GetCultureInfo((node as ILiteralNode).Language), (node as ILiteralNode).Value);

    public static T ToEnum<T>(GraphWrapperNode? node) where T : Enum => (T)Enum.ToObject(typeof(T), node.AsValuedNode().AsInteger());

    public static T EnumFromName<T>(GraphWrapperNode? node) where T : Enum => (T)Enum.Parse(typeof(T), (node as ILiteralNode).Value);

    public static Uri UriFromStringLiteral(GraphWrapperNode? node) => new((node as ILiteralNode).Value);

    public static DateTimeOffset DateTimeOffsetFromStringLiteral(GraphWrapperNode? node) => DateTimeOffset.Parse((node as ILiteralNode).Value);

    public static Regex RegexFromStringLiteral(GraphWrapperNode? node) => new((node as ILiteralNode).Value);

    public static IGraph? GraphFromGraphLiteral(GraphWrapperNode? node) => (node as IGraphLiteralNode)?.SubGraph;

    public static GraphPattern GraphPatternFromGraphLiteral(GraphWrapperNode? node)
    {
        if (node is not IGraphLiteralNode { NodeType: NodeType.GraphLiteral } graphLiteral)
        {
            throw new Exception("unknown node type");
        }

        var describe = QueryBuilder.Describe(Array.Empty<string>());
        foreach (var t in graphLiteral.SubGraph.Triples)
        {
            describe.Root.Where(b => b
                .Subject(Convert(t.Subject))
                .Predicate(Convert(t.Predicate))
                .Object(Convert(t.Object)));
        }

        return describe.BuildQuery().RootGraphPattern;
    }

    private static PatternItem Convert(INode node)
    {
        return node switch
        {
            VariableNode { NodeType: NodeType.Variable } v => new VariablePattern(v.VariableName),
            INode
            {
                NodeType: NodeType.Uri or
                NodeType.Blank or
                NodeType.Literal
            } n => new NodeMatchPattern(n),
            _ => throw new Exception("unknown node type")
        };
    }

    public static ValueMapping<T> EnumFromUri<T>(string prefix) where T : Enum => node =>
    {
        if (node is null)
        {
            return default;
        }

        if (node is not IUriNode { Uri: var uri })
        {
            throw new Exception("node is not a URI node");
        }

        string localPart() => prefix.EndsWith(hash)
            ? uri.ToString().Replace(prefix, string.Empty)
            : new Uri(prefix).MakeRelativeUri(uri).ToString();

        return (T)Enum.Parse(typeof(T), localPart());
    };

    public static ValueMapping<IList<T>> AsList<T>(GraphWrapperNode subject, INode predicate, NodeMapping<T> nmap, ValueMapping<T> vmap) =>
        node => new RdfCollectionList<T>(node, subject, predicate, nmap, vmap);
}
