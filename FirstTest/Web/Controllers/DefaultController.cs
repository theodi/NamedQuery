using Microsoft.AspNetCore.Mvc;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Web.Controllers;

[Route("/")]
public class DefaultController
{
	private const string ManifestName = "manifest.ttl";
	private static readonly Uri FakeBase = new("http://resources/");

	[HttpGet]
	public IEnumerable<string> Get()
	{
		using var reader = Resources.Reader(ManifestName) ?? throw new Exception("Manifest not found");
		using var graph = new Graph();
		new TurtleParser().Load(graph, reader);

		var sparqlFiles = graph.GetTriplesWithPredicate(graph.CreateUriNode(new Uri($"http://example.org/namedquery/sparql")))
			.Select(triple => triple.Object)
			.OfType<ILiteralNode>()
			.Select(literal => literal.Value)
			.Select(value => new Uri(value, UriKind.Relative))
			.Select(relative => new Uri(FakeBase, relative))
			.Select(absolute => absolute.GetComponents(UriComponents.Path, UriFormat.Unescaped));

		foreach (var sparqlFile in sparqlFiles)
		{
			yield return Resources.String(sparqlFile) ?? throw new Exception($"SPARQL file not found: {sparqlFile}");
		}
	}
}
