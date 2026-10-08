using Microsoft.Extensions.Options;
using DotNetRDF = VDS.RDF.Query;

namespace Web;

public class SparqlQueryClient(HttpClient httpClient, IOptions<Model.Options> options) : DotNetRDF.SparqlQueryClient(httpClient, options.Value.SparqlEndpoint);
