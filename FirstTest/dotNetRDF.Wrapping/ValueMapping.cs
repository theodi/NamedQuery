using VDS.RDF;

namespace VDS.RDF.Wrapping;

/// <summary>
/// 
/// </summary>
/// <remarks>Implementors must handle the case of <c>null</c> <paramref name="node"/></remarks>
/// <typeparam name="T"></typeparam>
/// <param name="node"></param>
/// <returns></returns>
public delegate T? ValueMapping<T>(GraphWrapperNode? node);
