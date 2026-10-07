using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Route("/")]
public class DefaultController
{
    private static readonly Uri FakeBase = new("http://resources/");

    [HttpGet]
    public IEnumerable<string> Get()
    {
        var sparqlFiles = Resources.Manifest.Endpoints
            .Select(endpoint => endpoint.Sparql)
            .Select(relative => new Uri(FakeBase, relative))
            .Select(absolute => absolute.GetComponents(UriComponents.Path, UriFormat.Unescaped));

        foreach (var sparqlFile in sparqlFiles)
        {
            yield return Resources.String(sparqlFile) ?? throw new Exception($"SPARQL file not found: {sparqlFile}");
        }
    }
}
