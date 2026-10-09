using Web.Model;

namespace Web.Middleware;

internal class PopulateEndpointInContextMiddleware(Context context) : IMiddleware
{
    async Task IMiddleware.InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        if (EndpointRoute.TryExtractPath(httpContext, out var path))
        {
            context.Endpoint = Resources.Manifest[path];
        }

        await next(httpContext);
    }
}
