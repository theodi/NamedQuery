using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Web.Controllers;

[Route("/")]
public class DefaultController(IOptions<Options> options)
{
	private const string ManifestName = "manifest.ttl";

	private readonly Options options = options.Value;

	[HttpGet]
	public IEnumerable<string> Get()
	{
		var basePath = options.BasePath;
		var manifestPath = Path.Combine(basePath, ManifestName);
		var sparqlFiles = File.ReadAllLines(manifestPath);

		foreach (var sparqlFile in sparqlFiles)
		{
			var sparqlPath = Path.Combine(basePath, sparqlFile);
			yield return File.ReadAllText(sparqlPath);
		}
	}
}
