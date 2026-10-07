using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Wrapping;

internal static class Extensions
{
    internal static GraphWrapperNode In(this INode node, IGraph graph) => node switch
    {
        GraphWrapperNode { Graph: var otherGraph } nodeWithGraph when ReferenceEquals(otherGraph, graph) => nodeWithGraph,
        _ => new(node, graph),
    };

    internal static IEnumerable<GraphWrapperNode> In(this IEnumerable<INode> nodes, IGraph graph) => nodes.Select(node => node.In(graph));

    internal static object? AsObject(this INode node) =>
        node.AsValuedNode() switch
        {
            null => null,
            IUriNode uriNode => uriNode.Uri,
            DoubleNode doubleNode => doubleNode.AsDouble(),
            FloatNode floatNode => floatNode.AsFloat(),
            DecimalNode decimalNode => decimalNode.AsDecimal(),
            BooleanNode booleanNode => booleanNode.AsBoolean(),
            DateTimeNode dateTimeNode => dateTimeNode.AsDateTimeOffset(),
            TimeSpanNode timeSpanNode => timeSpanNode.AsTimeSpan(),
            NumericNode numericNode => numericNode.AsInteger(),
            StringNode stringNode when stringNode.DataType.AbsoluteUri.Equals(XmlSpecsHelper.XmlSchemaDataTypeString) => stringNode.AsString(),
            StringNode stringNode when stringNode.DataType.AbsoluteUri.Equals(XmlSpecsHelper.XmlSchemaDataTypeAnyUri) => new Uri(stringNode.Value, UriKind.RelativeOrAbsolute),
            _ => node,
        };

    internal static INode NodeIn(this PatternItem pattern, IGraph graph) => pattern switch
    {
        VariablePattern variable => graph.CreateVariableNode(variable.VariableName),
        NodeMatchPattern nodeMatch => nodeMatch.Node,
        _ => throw new Exception("unexpected pattern item type")
    };
}
