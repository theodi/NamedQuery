using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace TheODI.NamedQuery.Model;

internal partial class Endpoint
{
    private static readonly Uri FakeBase = new("http://resources/");

    public string SparqlResourceName => new Uri(FakeBase, Sparql).GetComponents(UriComponents.Path, UriFormat.Unescaped);

    public string? FrameResourceName => Frame is { } frame ? new Uri(FakeBase, frame).GetComponents(UriComponents.Path, UriFormat.Unescaped) : null;

    public string ExampleUrl => QueryHelpers.AddQueryString(Path, Parameters.ToDictionary(static p => p.Name, static p => (string?)p.Example));

    public string ToolName => InvalidToolNameCharacter().Replace(Path, "_");

    [GeneratedRegex("[^A-Za-z0-9_-]")]
    private static partial Regex InvalidToolNameCharacter();
}
