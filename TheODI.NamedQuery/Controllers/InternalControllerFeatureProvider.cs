using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace TheODI.NamedQuery.Controllers;

internal class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo) =>
        typeInfo.Assembly == typeof(InternalControllerFeatureProvider).Assembly &&
        typeInfo is { IsClass: true, IsAbstract: false, IsPublic: false, IsNested: false, ContainsGenericParameters: false } &&
        typeInfo.Name.EndsWith("Controller", StringComparison.Ordinal);
}
