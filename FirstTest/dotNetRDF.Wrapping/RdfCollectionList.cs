using System.Collections;
using VDS.RDF;

namespace VDS.RDF.Wrapping;

internal class RdfCollectionList<T>(INode? root, GraphWrapperNode subject, INode predicate, NodeMapping<T> toNode, ValueMapping<T> toValue) : IList<T>
{
    private readonly INode subject = subject ?? throw new ArgumentNullException(nameof(subject));
    private readonly INode predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    private readonly NodeMapping<T> toNode = toNode ?? throw new ArgumentNullException(nameof(toNode));
    private readonly ValueMapping<T> toValue = toValue ?? throw new ArgumentNullException(nameof(toValue));
    private readonly IGraph graph = subject.Graph ?? throw new ArgumentException("must have graph", nameof(subject));
    private INode? root = root switch
    {
        null => null,
        var root when root.Equals(Vocabulary.Nil) => root,
        var root when !root.IsListRoot(subject.Graph) => throw new ArgumentException("must be list", nameof(root)),
        _ => root,
    };

    public T this[int index]
    {
        get => Values.ElementAt(index);

        set
        {
            RemoveAt(index);
            Insert(index, value);
        }
    }

    public int Count => Items.Count();

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        if (IsEmpty)
        {
            RemoveAnchorTriple();
        }

        if (root is null || IsEmpty)
        {
            root = graph.AssertList([item], NodeFrom);
            AddAnchorTriple();
            return;
        }

        graph.AddToList(root, [item], NodeFrom);
    }

    public void Clear()
    {
        // If our triple doesn't exist then let's not create one.
        if (root is null)
        {
            return;
        }

        // If the underlying list is already empty then no need to do anything.
        if (IsEmpty)
        {
            return;
        }

        graph.RetractList(root); // Remove all the `first` & `rest` triples.
        RemoveAnchorTriple();
        MakeEmpty();
        AddAnchorTriple();
    }

    private void MakeEmpty() => root = Vocabulary.Nil;
    private bool IsEmpty => Vocabulary.Nil.Equals(root);

    public bool Contains(T item) => Values.Contains(item);

    public void CopyTo(T[] array, int index) => Values.ToArray().CopyTo(array, index);

    public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int IndexOf(T item)
    {
        var index = 0;

        foreach (var value in Values)
        {
            if (Equals(item, value))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public void Insert(int index, T item)
    {
        var items = Items.ToList();
        items.Insert(index, NodeFrom(item));
        Clear();

        if (root is not null)
        {
            RemoveAnchorTriple();
        }

        root = graph.AssertList(items);
        AddAnchorTriple();
    }

    public bool Remove(T item)
    {
        if (!Contains(item))
        {
            return false;
        }

        graph.RemoveFromList(root, [item], NodeFrom);

        if (!root.IsListRoot(graph))
        {
            RemoveAnchorTriple();
            MakeEmpty();
            AddAnchorTriple();
        }

        return true;
    }

    public void RemoveAt(int index)
    {
        var items = Items.ToList();
        items.RemoveAt(index);
        Clear();

        RemoveAnchorTriple();
        root = graph.AssertList(items);
        AddAnchorTriple();
    }

    private IEnumerable<GraphWrapperNode> Items => root switch
    {
        null => [],
        var _ when IsEmpty => [],
        _ => graph.GetListItems(root).In(graph),
    };

    private IEnumerable<T> Values => Items.Select(item => toValue(item)!);

    private GraphWrapperNode NodeFrom(T item) => toNode(item, graph);

    private void RemoveAnchorTriple() => graph.Retract(subject, predicate, root);

    private void AddAnchorTriple() => graph.Assert(subject, predicate, root);
}
