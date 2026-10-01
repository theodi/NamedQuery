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
		if (Resources.Info("manifest.ttl") is null)
		{
			throw new MsBuildCanonicalErrorException(ManifestName, "Manifest file not found in endpoint definition folder");
		}
	}
}
