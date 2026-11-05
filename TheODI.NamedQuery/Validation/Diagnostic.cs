namespace TheODI.NamedQuery.Validation;

internal record Diagnostic(string Code, string Path, string Message)
{
    internal int? Line { get; init; }

    internal int? Column { get; init; }

    internal int? EndLine { get; init; }

    internal int? EndColumn { get; init; }

    public override string ToString()
    {
        var location = (Line, Column, EndLine, EndColumn) switch
        {
            ({ } line, { } column, { } endLine, { } endColumn) => $"({line},{column},{endLine},{endColumn})",
            ({ } line, { } column, _, _) => $"({line},{column})",
            ({ } line, _, _, _) => $"({line})",
            _ => ""
        };

        return $"{Path}{location} : error {Code} : {Message}";
    }
}
