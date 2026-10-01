namespace ValidateResources;

internal static class Validator
{
	internal static void Validate()
	{
		ManifestExists();
	}

	private static void ManifestExists()
	{
		var manifest = Web.Resources.Manifest;
		if (string.IsNullOrEmpty(manifest))
		{
			throw new InvalidOperationException("Manifest file not found in endpoint definition folder");
		}
	}
}
