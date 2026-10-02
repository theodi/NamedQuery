using System.Reflection;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Shacl;
using Web;

namespace ValidateResources;

internal static class Validator
{
	private const string ManifestName = "manifest.ttl";
	private const string ShapesName = "ValidateResources.shapes.ttl";

	internal static void Validate()
	{
		ManifestExists();
		ManifestIsValidTurtle();
		ManifestConformsToShapes();
		SparqlIsRelativeUri();
		// TODO: nq:sparql files exist
		// TODO: nq:sparql files are valid sparql
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

	private static void ManifestConformsToShapes()
	{
		var dataGraph = new Graph();
		new TurtleParser().Load(dataGraph, Resources.Reader(ManifestName));

		var shapesGraph = new ShapesGraph(new Graph());
		using var shapesReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ShapesName)!);
		new TurtleParser().Load(shapesGraph, shapesReader);

		var report = shapesGraph.Validate(dataGraph);
		if (report.Conforms) return;

		var violations = report.Results.Select(result => $"{result.FocusNode}: {result.Message?.Value ?? result.SourceConstraintComponent.ToString()}");
		throw new MsBuildCanonicalErrorException(ManifestName, $"Manifest does not conform to shapes: {string.Join("; ", violations)}");
	}

	private static void SparqlIsRelativeUri()
	{
		using var graph = new Graph();
		new TurtleParser().Load(graph, Resources.Reader(ManifestName));

		var invalid = graph.GetTriplesWithPredicate(graph.CreateUriNode(new Uri("http://example.org/namedquery/sparql")))
			.Select(triple => triple.Object)
			.OfType<ILiteralNode>()
			.Select(literal => literal.Value)
			.Where(value => !Uri.TryCreate(value, UriKind.Relative, out _))
			.ToList();

		if (invalid.Count == 0) return;

		throw new MsBuildCanonicalErrorException(ManifestName, $"Endpoint SPARQL must be relative URI: {string.Join("; ", invalid)}");
	}
}
