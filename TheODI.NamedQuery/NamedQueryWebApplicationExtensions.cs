using TheODI.NamedQuery;

namespace Microsoft.AspNetCore.Builder;

public static class NamedQueryWebApplicationExtensions
{
    public static WebApplication UseNamedQuery(this WebApplication app)
    {
        app.UseSwaggerUI();
        app.UseCors();
        app.MapControllers();
        app.MapRazorPages();
        app.MapMcp(Mcp.Route);

        return app;
    }
}
