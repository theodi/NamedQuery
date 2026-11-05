namespace TheODI.NamedQuery.Validation;

internal record Diagnostic(string Code, string Path, string Message)
{
    internal int? Line { get; init; }

    internal int? Column { get; init; }

    internal int? EndLine { get; init; }

    internal int? EndColumn { get; init; }
}
