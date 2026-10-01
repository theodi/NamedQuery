using System.Reflection;

namespace Web;

public static class Resources
{
	public static string Manifest => String("manifest.ttl");

	internal static Stream? Stream(string name) =>
		Assembly.GetExecutingAssembly().GetManifestResourceStream($"Web.Resources.{name}");

	internal static StreamReader? Reader(string name) =>
		Stream(name) switch
		{
			null => null,
			var stream => new StreamReader(stream)
		};

	internal static string? String(string name)
	{
		using var reader = Reader(name);
		return reader?.ReadToEnd();
	}
}