using System.Threading.Tasks;

using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes returning HTTP 409 status
	/// </summary>
	/// <remarks>
	///		Converts <see cref="Task{TResult}"/> of <see cref="IDomainResult{T}"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Conflict' to <see cref="Microsoft.AspNetCore.Http.Result.ConflictObjectResult"/>
	/// </remarks>
	public static void MapConflictResponses(this IEndpointRouteBuilder app)
	{
		DomainConflictService service = new();
		
		var routes = new [] 
			{
				app.MapGet("GetConflictWithNoMessage",	() => service.GetConflictWithNoMessage().ToResult()),
				app.MapGet("GetConflictWithMessage",		() => service.GetConflictWithMessage().ToResult()),
				
				app.MapGet("GetConflictWithNoMessageWhenExpectedNumber",	() => service.GetConflictWithNoMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetConflictWithMessageWhenExpectedNumber",	() => service.GetConflictWithMessageWhenExpectedNumber().ToResult()),
				
				app.MapGet("GetConflictWithNoMessageWhenExpectedNumberTuple",() => service.GetConflictWithNoMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetConflictWithMessageWhenExpectedNumberTuple",	() => service.GetConflictWithMessageWhenExpectedNumberTuple().ToResult())
			};
		
		foreach (var route in routes)                               
		   route.WithTags("Failed: 409 Conflict")
				.ProducesProblem(StatusCodes.Status409Conflict);
	}
}
