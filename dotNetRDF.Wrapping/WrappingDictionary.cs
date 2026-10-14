using System.Collections;
using VDS.RDF;

namespace VDS.RDF.Wrapping;

public class WrappingDictionary<T, K>(GraphWrapperNode subject, INode predicate, NodeMapping<KeyValuePair<T, K>> nodeMapping, ValueMapping<KeyValuePair<T, K>> valueMapping) : IDictionary<T, K>
{
    public K this[T key]
    {
        get => subject.Graph.GetTriplesWithSubjectPredicate(subject, predicate)
            .Select(t => t.Object)
            .Select(obj => valueMapping(obj.In(subject.Graph)))
            .Where(k => EqualityComparer<T>.Default.Equals(k.Key, key))
            .Select(x => x.Value)
            .Single();

        set
        {
            Remove(key);
            Add(key, value);
        }
    }

    public ICollection<T> Keys => subject.Graph.GetTriplesWithSubjectPredicate(subject, predicate)
        .Select(t => t.Object)
        .Select(obj => valueMapping(obj.In(subject.Graph)))
        .Select(x => x.Key)
        .Distinct()
        .ToList();

    public ICollection<K> Values => subject.Graph.GetTriplesWithSubjectPredicate(subject, predicate)
        .Select(t => t.Object)
        .Select(obj => valueMapping(obj.In(subject.Graph)))
        .Select(l => l.Value)
        .ToList();

    public int Count => subject.Graph.GetTriplesWithSubjectPredicate(subject, predicate).Count();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(T key, K value) => Add(new KeyValuePair<T, K>(key, value));

    public void Add(KeyValuePair<T, K> item) => subject.Graph.Assert(subject, predicate, nodeMapping(item, subject.Graph));

    public void Clear()
    {
        subject.Graph.Retract(subject.Graph.GetTriplesWithSubjectPredicate(subject, predicate));
    }

    public bool Contains(KeyValuePair<T, K> item)
    {
        throw new NotImplementedException();
    }

    public bool ContainsKey(T key)
    {
        return subject.Graph
            .GetTriplesWithSubjectPredicate(subject, predicate)
            .Select(t => t.Object)
            .Select(o => o.In(subject.Graph))
            .Select(o => valueMapping(o))
            .Select(v => v.Key)
            .Any(k => EqualityComparer<T>.Default.Equals(k, key));
    }

    public void CopyTo(KeyValuePair<T, K>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<T, K>> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public bool Remove(T key)
    {
        var contains = ContainsKey(key);

        var t = subject.Graph
            .GetTriplesWithSubjectPredicate(subject, predicate)
            .Where(t => EqualityComparer<T>.Default.Equals(valueMapping(t.Object.In(subject.Graph)).Key, key));
        subject.Graph.Retract(t);

        return contains;
    }

    public bool Remove(KeyValuePair<T, K> item)
    {
        throw new NotImplementedException();
    }

    public bool TryGetValue(T key, out K value)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
