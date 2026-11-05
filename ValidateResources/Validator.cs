extern alias App;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppProgram = App::Program;

namespace ValidateResources;

internal static class Validator
{
    private static readonly WebApplicationFactory<AppProgram> factory = Factory();

    internal static IEnumerable<Diagnostic> Validate() => TheODI.NamedQuery.Validation.Validator.Validate(factory.Services)
        .Select(diagnostic => new Diagnostic(diagnostic.Path, diagnostic.Message)
        {
            Code = diagnostic.Code,
            Line = diagnostic.Line,
            Column = diagnostic.Column,
            EndLine = diagnostic.EndLine,
            EndColumn = diagnostic.EndColumn
        });

    private static WebApplicationFactory<AppProgram> Factory()
    {
        Environment.SetEnvironmentVariable($"ASPNETCORE_TEST_CONTENTROOT_{typeof(AppProgram).Assembly.GetName().Name!.ToUpperInvariant().Replace('.', '_')}", AppContext.BaseDirectory);

        return new WebApplicationFactory<AppProgram>().WithWebHostBuilder(builder => builder
            .UseSetting("NamedQuery:SparqlEndpoint", "http://localhost/")
            .ConfigureLogging(logging => logging.ClearProviders()));
    }
}
