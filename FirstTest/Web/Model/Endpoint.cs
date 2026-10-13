namespace Web.Model;

public partial class Endpoint
{
    private static readonly Uri FakeBase = new("http://resources/");

    public string SparqlResourceName => new Uri(FakeBase, Sparql).GetComponents(UriComponents.Path, UriFormat.Unescaped);

    public string? FrameResourceName => Frame switch
    {
        null => null,
        var frame => new Uri(FakeBase, frame).GetComponents(UriComponents.Path, UriFormat.Unescaped)
    };
}
