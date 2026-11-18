using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace TheODI.NamedQuery.Controllers;

// Removes this library's controllers while leaving the host application's own, for when the REST API is turned off.
internal class RemoveNamedQueryControllersConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers.Where(static controller => controller.ControllerType.Assembly == typeof(RemoveNamedQueryControllersConvention).Assembly).ToList())
        {
            application.Controllers.Remove(controller);
        }
    }
}
