extern alias App;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using TheODI.NamedQuery.Validation;
using AppProgram = App::Program;

try
{
    Environment.SetEnvironmentVariable($"ASPNETCORE_TEST_CONTENTROOT_{typeof(AppProgram).Assembly.GetName().Name!.ToUpperInvariant().Replace('.', '_')}", AppContext.BaseDirectory);

    using var factory = new WebApplicationFactory<AppProgram>().WithWebHostBuilder(builder => builder
        .UseSetting("NamedQuery:SparqlEndpoint", "http://localhost/")
        .ConfigureLogging(logging => logging.ClearProviders()));

    var diagnostics = Validator.Validate(factory.Services);

    foreach (var diagnostic in diagnostics)
    {
        Console.Error.WriteLine(diagnostic);
    }

    return diagnostics.Count == 0 ? 0 : 1;
}
catch (Exception e)
{
    Console.Error.WriteLine(new Diagnostic(DiagnosticCodes.Internal, "ValidateResources", (e is TypeInitializationException { InnerException: { } inner } ? inner : e).Message));
    return 1;
}
