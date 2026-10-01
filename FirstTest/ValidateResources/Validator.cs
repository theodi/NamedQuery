using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using Web;

namespace ValidateResources;

internal static class Validator
{
	private const string ManifestName = "manifest.ttl";

	internal static void Validate()
	{
		ManifestExists();
		ManifestIsValidTurtle();
	}

	private static void ManifestExists()
	{
		if (Resources.Info("manifest.ttl") is null)
		{
			throw new MsBuildCanonicalErrorException(ManifestName, "Manifest file not found in endpoint definition folder");
		}
	}

	private static void ManifestIsValidTurtle()
	{
		try
		{
			new TurtleParser().Load(new NullHandler(), Resources.Reader("manifest.ttl"));
		}
		catch (RdfParseException e)
		{
			throw new MsBuildCanonicalErrorException(ManifestName, $"Manifest is not valid Turtle: {e.Message}", e)
			{
				Line = e.HasPositionInformation ? e.StartLine : null,
				Column = e.HasPositionInformation ? e.StartPosition : null,
				EndLine = e.HasPositionInformation ? e.EndLine : null,
				EndColumn = e.HasPositionInformation ? e.EndPosition : null
			};
		}
	}
}
