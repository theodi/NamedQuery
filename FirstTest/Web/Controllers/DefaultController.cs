using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Route("/")]
public class DefaultController
{
	private const string ManifestName = "manifest.ttl";

	[HttpGet]
	public IEnumerable<string> Get()
	{
		var sparqlFiles = Resources.String(ManifestName).Split(Environment.NewLine);

		foreach (var sparqlFile in sparqlFiles)
		{
			yield return Resources.String(sparqlFile);
		}
	}
}
