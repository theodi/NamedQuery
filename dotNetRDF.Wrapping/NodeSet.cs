using System.Collections;
using VDS.RDF;
using VDS.RDF.Writing;

namespace VDS.RDF.Wrapping;

internal class NodeSet<T>(GraphWrapperNode anchor, INode predicate, TripleSegment segment, NodeMapping<T> toNode, ValueMapping<T> toValue) : ISet<T>
{
    private readonly GraphWrapperNode anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
    private readonly INode predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    private readonly TripleSegment segment = segment switch
    {
        TripleSegment.Subject
        or TripleSegment.Object => segment,
        _ => throw new ArgumentException("must be s or o", nameof(segment)),
    };
    private readonly NodeMapping<T> toNode = toNode ?? throw new ArgumentNullException(nameof(toNode));
    private readonly ValueMapping<T> toValue = toValue ?? throw new ArgumentNullException(nameof(toValue));
    private readonly IGraph graph = anchor.Graph ?? throw new ArgumentException("must have graph", nameof(anchor));

    public int Count => Statements.Count();

    public bool IsReadOnly => false;

    public bool Add(T item) => graph.Assert(StatementFrom(item));

    void ICollection<T>.Add(T item) => Add(item);

    public void Clear() => graph.Retract(Statements);

    public bool Contains(T item) => Values.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => Values.ToArray().CopyTo(array, arrayIndex);

    public bool Remove(T item) => graph.Retract(StatementFrom(item));

    public void ExceptWith(IEnumerable<T> other) => graph.Retract(StatementsFrom(other));

    public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void IntersectWith(IEnumerable<T> other) => graph.Retract(Statements.Except(StatementsFrom(other)));

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        var otherStatements = StatementsFrom(other);
        var intersecting = Statements.Intersect(otherStatements).ToArray();

        graph.Assert(otherStatements);
        graph.Retract(intersecting);
    }

    public void UnionWith(IEnumerable<T> other) => graph.Assert(StatementsFrom(other));

    public bool IsProperSubsetOf(IEnumerable<T> other) => IsSubsetOf(other) && !IsSupersetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other) => IsSupersetOf(other) && !IsSubsetOf(other);

    public bool IsSubsetOf(IEnumerable<T> other) => Values.All(other.Contains);

    public bool IsSupersetOf(IEnumerable<T> other) => other.All(Contains);

    public bool Overlaps(IEnumerable<T> other) => other.Any(Contains) || Values.Any(other.Contains);

    public bool SetEquals(IEnumerable<T> other) => IsSupersetOf(other) && IsSubsetOf(other);

    private IEnumerable<Triple> Statements => segment switch
    {
        TripleSegment.Subject => graph.GetTriplesWithSubjectPredicate(anchor, predicate),
        TripleSegment.Object or _ => graph.GetTriplesWithPredicateObject(predicate, anchor),
    };

    private IEnumerable<GraphWrapperNode> Nodes => segment switch
    {
        TripleSegment.Subject => Statements.Select(statement => statement.Object).In(graph),
        TripleSegment.Object or _ => Statements.Select(statement => statement.Subject).In(graph),
    };

    private IEnumerable<T?> Values => Nodes.Select(toValue.Invoke);

    private Triple StatementFrom(T value) => segment switch
    {
        TripleSegment.Subject => new(anchor, predicate, NodeFrom(value)),
        TripleSegment.Object or _ => new(NodeFrom(value), predicate, anchor),
    };

    private INode NodeFrom(T value) => value switch
    {
        null => throw new ArgumentNullException(nameof(value)),
        _ => toNode(value, graph)
    };

    private IEnumerable<Triple> StatementsFrom(IEnumerable<T> other) => other switch
    {
        null => throw new ArgumentNullException(nameof(other)),
        NodeSet<T> otherSet => otherSet.Statements,
        IGraph otherGraph => otherGraph.Triples,
        IEnumerable<Triple> otherTriples => otherTriples,
        _ => other.Select(StatementFrom)
    };
}
