using Microsoft.Extensions.Options;
using DotNetRDF = VDS.RDF.Query;

namespace TheODI.NamedQuery;

internal class SparqlQueryClient(HttpClient httpClient, IOptions<NamedQueryOptions> options) : DotNetRDF.SparqlQueryClient(httpClient, options.Value.SparqlEndpoint);
