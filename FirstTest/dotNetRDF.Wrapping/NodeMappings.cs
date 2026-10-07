using System.Globalization;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Wrapping;

public static class NodeMappings
{
    public static GraphWrapperNode From<T>(T value, IGraph graph) // TODO: Rename
    {
        var node = value switch
        {
            null => throw new InvalidOperationException(), // TODO: describe
            INode nodeValue => nodeValue,
            Uri uriValue => new UriNode(uriValue),
            bool boolValue => new BooleanNode(boolValue),
            byte byteValue => new ByteNode(byteValue),
            DateTime dateTimeValue => new DateTimeNode(dateTimeValue),
            DateTimeOffset dateTimeOffsetValue => new DateTimeNode(dateTimeOffsetValue),
            decimal decimalValue => new DecimalNode(decimalValue),
            double doubleValue => new DoubleNode(doubleValue),
            float floatValue => new FloatNode(floatValue),
            long longValue => new LongNode(longValue),
            int intValue => new LongNode(intValue),
            string stringValue => new StringNode(stringValue),
            char charValue => new StringNode(charValue.ToString()),
            TimeSpan timeSpanValue => new TimeSpanNode(timeSpanValue),
            _ => throw new InvalidCastException($"Can't convert type {value.GetType()}"),
        };

        return node.In(graph);
    }

    public static GraphWrapperNode FromLangStringPair(KeyValuePair<CultureInfo, string> kv, IGraph graph) => graph.CreateLiteralNode(kv.Value, kv.Key.Name).In(graph);

    public static GraphWrapperNode NameFromEnum<T>(T? enumeration, IGraph graph) => From(Enum.GetName(Nullable.GetUnderlyingType(typeof(T)), enumeration), graph);

    public static NodeMapping<T> UriFromEnum<T>(string prefix) where T : Enum => (enumeration, graph) => From(new Uri(new Uri(prefix), Enum.GetName(typeof(T), enumeration)), graph);

    public static GraphWrapperNode StringLiteralFromUri(Uri value, IGraph graph) => From(value.AbsoluteUri, graph);

    public static GraphWrapperNode StringLiteralFromDateTimeOffset(DateTimeOffset? value, IGraph graph) => From(value?.ToString("yyyy-MM-dd'T'HH:mm:sszzz"), graph);

    public static GraphWrapperNode GraphLiteraFromGraph(IGraph value, IGraph graph) => graph.CreateGraphLiteralNode(value).In(graph);

    public static GraphWrapperNode GraphLiteraFromGraphPattern(GraphPattern value, IGraph graph)
    {
        var subgraph = new Graph();
        foreach (var pattern in value.TriplePatterns)
        {
            if (pattern is not TriplePattern triplePattern)
            {
                throw new Exception("unexpected triple pattern type");
            }

            subgraph.Assert(
                triplePattern.Subject.NodeIn(subgraph),
                triplePattern.Predicate.NodeIn(subgraph),
                triplePattern.Object.NodeIn(subgraph));
        }

        return graph.CreateGraphLiteralNode(subgraph).In(graph);
    }

    public static NodeMapping<IList<T>> AsList<T>(NodeMapping<T> map) => (value, graph) => graph.AssertList(value, item => map(item, graph)).In(graph);
}
