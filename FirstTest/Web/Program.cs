using DotNetRDF = VDS.RDF.Query;
using Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddOptions<Options>().BindConfiguration(Options.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddHttpClient<DotNetRDF.ISparqlQueryClient, SparqlQueryClient>();

var app = builder.Build();
app.MapControllers();
app.MapRazorPages();

app.Run();
