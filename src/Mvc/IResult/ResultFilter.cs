using System.Threading.Tasks;

using DomainResults.Common;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DomainResults.Mvc.IResult;

public class ResultFilter : IActionFilter, IEndpointFilter
{
	/// <summary>
	/// Called before the action executes, after model binding is complete.
	/// </summary>
	/// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext" />.</param>
	public void OnActionExecuting(ActionExecutingContext context) {}
	
	/// <summary>
	/// Called after the action executes, before the action result.
	/// </summary>
	/// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext" />.</param>
	public void OnActionExecuted(ActionExecutedContext context)
	{
		var actionResult = (context.Result as ObjectResult)?.Value;
		if (actionResult is IDomainResult result)
			context.Result = result.ToActionResult();
	//	else (actionResult is IDomainResult<> resultGeneric)
	//	context.Result = resultGeneric.ToActionResult();
	}
	
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		// See context here
		return await next(context);
	}
}
