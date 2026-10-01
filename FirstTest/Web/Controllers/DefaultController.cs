using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Route("/")]
public class DefaultController
{
	private const string ManifestName = "manifest.ttl";

	[HttpGet]
	public IEnumerable<string> Get()
	{
		var sparqlFiles = Resources.String(ManifestName)?.Split(Environment.NewLine) ?? throw new Exception("Manifest not found");

		foreach (var sparqlFile in sparqlFiles)
		{
			yield return Resources.String(sparqlFile) ?? throw new Exception($"SPARQL file not found: {sparqlFile}");
		}
	}
}
