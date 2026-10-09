using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Model;

namespace Web.Filters;

internal class PopulateEndpointInContextAttribute : TypeFilterAttribute<PopulateEndpointInContextAttribute.Filter>
{
    internal class Filter(Context context) : IResourceFilter
    {
        void IResourceFilter.OnResourceExecuting(ResourceExecutingContext resource)
        {
            if (resource.RouteData.Values["path"] is string path)
            {
                context.Endpoint = Resources.Manifest[path];
            }
        }

        void IResourceFilter.OnResourceExecuted(ResourceExecutedContext _) { }
    }
}
