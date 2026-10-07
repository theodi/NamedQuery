var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOptions<Web.Options>().BindConfiguration(Web.Options.SectionName).ValidateDataAnnotations().ValidateOnStart();

var app = builder.Build();
app.MapControllers();

app.Run();
