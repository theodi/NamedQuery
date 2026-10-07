using Web;
using DotNetRDF = VDS.RDF.Query;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddOptions<Options>().BindConfiguration(Options.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddHttpClient<DotNetRDF.ISparqlQueryClient, SparqlQueryClient>();

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.MapRazorPages();

app.Run();
