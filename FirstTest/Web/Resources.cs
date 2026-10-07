using System.Reflection;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Web;

public static class Resources
{
	private const string ManifestName = "manifest.ttl";

	private static Stream? Stream(string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

	public static ManifestResourceInfo? Info(string name) => Assembly.GetExecutingAssembly().GetManifestResourceInfo(name);

	public static StreamReader? Reader(string name) => Stream(name) switch
	{
		null => null,
		var stream => new StreamReader(stream)
	};

	internal static string? String(string name)
	{
		using var reader = Reader(name);
		return reader?.ReadToEnd();
	}

	public static Model.Manifest Manifest
	{
		get
		{
			using var reader = Reader(ManifestName) ?? throw new Exception("Manifest not found");
			var graph = new Graph();
			new TurtleParser().Load(graph, reader);
			return Model.Manifest.Wrap(graph);
		}
	}
}
