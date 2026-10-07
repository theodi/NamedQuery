namespace Web.Model;

public partial class Endpoint
{
	private static readonly Uri FakeBase = new("http://resources/");

	public string SparqlResourceName => new Uri(FakeBase, Sparql).GetComponents(UriComponents.Path, UriFormat.Unescaped);

	public string Query => Resources.String(SparqlResourceName) ?? throw new Exception($"SPARQL file not found: {SparqlResourceName}");
}
