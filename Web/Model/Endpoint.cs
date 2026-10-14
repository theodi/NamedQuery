using System.Text.RegularExpressions;

namespace Web.Model;

public partial class Endpoint
{
    private static readonly Uri FakeBase = new("http://resources/");

    public string SparqlResourceName => new Uri(FakeBase, Sparql).GetComponents(UriComponents.Path, UriFormat.Unescaped);

    public string? FrameResourceName => Frame is { } frame ? new Uri(FakeBase, frame).GetComponents(UriComponents.Path, UriFormat.Unescaped) : null;

    public string ToolName => InvalidToolNameCharacter().Replace(Path, "_");

    [GeneratedRegex("[^A-Za-z0-9_-]")]
    private static partial Regex InvalidToolNameCharacter();
}
