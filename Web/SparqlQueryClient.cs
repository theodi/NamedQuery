using Microsoft.Extensions.Options;
using Web.Model;
using DotNetRDF = VDS.RDF.Query;

namespace Web;

public class SparqlQueryClient(HttpClient httpClient, IOptions<NamedQueryOptions> options) : DotNetRDF.SparqlQueryClient(httpClient, options.Value.SparqlEndpoint);
