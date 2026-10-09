using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Web.Filters;

internal class Respond400IfInvalidModelStateAttribute : TypeFilterAttribute<Respond400IfInvalidModelStateAttribute.Filter>
{
    internal class Filter(ProblemDetailsFactory problemDetailsFactory) : IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext action)
        {
            if (action.ModelState.IsValid)
            {
                return;
            }

            var problem = problemDetailsFactory.CreateValidationProblemDetails(action.HttpContext, action.ModelState);

            // JsonResult instead of BadRequestObjectResult because latter would conneg, leading to potential 406
            action.Result = new JsonResult(problem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentType = MediaTypeNames.Application.ProblemJson
            };
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext _) { }
    }
}
