using Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOptions<Options>()
	.BindConfiguration(Options.Section)
	.ValidateDataAnnotations()
	.ValidateOnStart();

var app = builder.Build();
app.MapControllers();

app.Run();
