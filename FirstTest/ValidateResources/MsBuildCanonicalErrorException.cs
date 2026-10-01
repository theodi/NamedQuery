namespace ValidateResources;

/// <seealso href="https://learn.microsoft.com/visualstudio/msbuild/msbuild-diagnostic-format-for-tasks"/>
internal class MsBuildCanonicalErrorException(string origin, string text, Exception? innerException = null) : Exception(text, innerException)
{
	internal string Origin { get; } = origin;

	internal int? Line { get; init; }

	internal int? Column { get; init; }

	internal int? EndLine { get; init; }

	internal int? EndColumn { get; init; }

	internal string? Subcategory { get; init; }

	internal MsBuildCategory Category { get; init; } = MsBuildCategory.Error;

	internal string? Code { get; init; }

	public override string ToString()
	{
		var location = (Line, Column, EndLine, EndColumn) switch
		{
			({ } line, { } column, { } endLine, { } endColumn) => $"({line},{column},{endLine},{endColumn})",
			({ } line, { } column, _, _)  => $"({line},{column})",
			({ } line, _, _, _) => $"({line})",
			_ => ""
		};

		var category = Category switch
		{
			MsBuildCategory.Warning => "warning",
			_ => "error"
		};

		var classification = string.Join(" ", new[] { Subcategory, category, Code }.Where(part => !string.IsNullOrEmpty(part)));

		return $"{Origin}{location} : {classification} : {Message}";
	}

	internal enum MsBuildCategory
	{
		Error,
		Warning
	}
}
