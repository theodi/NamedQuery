using System.Reflection;

namespace Web;

public static class Resources
{
	private static string Name(string name) => $"Web.Resources.{name}";

	private static Stream? Stream(string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream(Name(name));

	public static ManifestResourceInfo? Info(string name) => Assembly.GetExecutingAssembly().GetManifestResourceInfo(Name(name));

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
}
