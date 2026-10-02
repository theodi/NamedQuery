using Microsoft.AspNetCore.Mvc;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Web.Controllers;

[Route("/")]
public class DefaultController
{
	private const string ManifestName = "manifest.ttl";

	[HttpGet]
	public IEnumerable<string> Get()
	{
		using var reader = Resources.Reader(ManifestName) ?? throw new Exception("Manifest not found");
		using var graph = new Graph();
		new TurtleParser().Load(graph, reader);

		var sparqlFiles = graph.GetTriplesWithPredicate(graph.CreateUriNode(new Uri($"http://example.org/namedquery/sparql")))
			.Select(triple => triple.Object)
			.OfType<ILiteralNode>()
			.Select(literal => new Uri(new("file:///"), literal.Value).AbsolutePath.TrimStart('/'));

		foreach (var sparqlFile in sparqlFiles)
		{
			yield return Resources.String(sparqlFile) ?? throw new Exception($"SPARQL file not found: {sparqlFile}");
		}
	}
}
