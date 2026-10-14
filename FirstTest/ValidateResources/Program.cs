using ValidateResources;

const string Origin = "ValidateResources";

try
{
    var diagnostics = Validator.Validate().ToList();

    foreach (var diagnostic in diagnostics)
    {
        Console.Error.WriteLine(diagnostic);
    }

    return diagnostics.Count == 0 ? 0 : 1;
}
catch (TypeInitializationException e) when (e.InnerException is not null)
{
    Console.Error.WriteLine(new Diagnostic(Origin, e.InnerException.Message));
    return 1;
}
catch (Exception e)
{
    Console.Error.WriteLine(new Diagnostic(Origin, e.Message));
    return 1;
}
