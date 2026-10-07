using VDS.RDF;

namespace VDS.RDF.Wrapping;

public delegate GraphWrapperNode NodeMapping<T>(T value, IGraph graph);
