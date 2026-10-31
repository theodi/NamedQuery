using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNamedQuery(options => options.Definitions = new ManifestEmbeddedFileProvider(typeof(Program).Assembly));

var app = builder.Build();
app.UseNamedQuery();

app.Run();
