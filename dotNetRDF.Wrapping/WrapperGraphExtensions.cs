using VDS.RDF;

namespace VDS.RDF.Wrapping;

public static class WrapperGraphExtensions
{
    public static IEnumerable<T> SubjectsOf<T>(this IGraph graph, INode predicate, Func<GraphWrapperNode, T> map) =>
        graph
            .GetTriplesWithPredicate(predicate)
            .Select(triple => triple.Subject)
            .Distinct()
            .In(graph)
            .Select(map);

    public static IEnumerable<T> InstancesOf<T>(this IGraph graph, INode type, Func<GraphWrapperNode, T> map) =>
        graph
            .GetTriplesWithPredicateObject(Vocabulary.RdfType, type)
            .Select(triple => triple.Subject)
            .Distinct()
            .In(graph)
            .Select(map);
}
