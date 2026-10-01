namespace ValidateResources;

public class Validator
{
	public void Validate()
	{
		var manifest = Web.Resources.Manifest;
		if (string.IsNullOrEmpty(manifest))
		{
			throw new InvalidOperationException("Manifest file not found in endpoint definition folder");
		}
	}
}
