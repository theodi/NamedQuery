using System.Reflection;

namespace Web;

internal static class Resources
{
	internal static Stream Stream(string name)
	{
		return Assembly.GetExecutingAssembly().GetManifestResourceStream($"Web.Resources.{name}");
	}

	internal static StreamReader Reader(string name)
	{
		return new StreamReader(Stream(name));
	}

	internal static string String(string name)
	{
		using var reader = Reader(name);
		return reader.ReadToEnd();
	}

}