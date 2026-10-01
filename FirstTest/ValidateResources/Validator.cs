namespace ValidateResources;

internal static class Validator
{
	private const string ManifestName = "manifest.ttl";

	internal static void Validate()
	{
		ManifestExists();
	}

	private static void ManifestExists()
	{
		var manifest = Web.Resources.Manifest;
		if (string.IsNullOrEmpty(manifest))
		{
			throw new MsBuildCanonicalErrorException(ManifestName, "Manifest file not found in endpoint definition folder");
		}
	}
}
