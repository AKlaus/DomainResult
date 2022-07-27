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
	///		Map all routes returning HTTP 403 status
	/// </summary>
	/// <remarks>
	///		Converts <see cref="Task{TResult}"/> of <see cref="IDomainResult{T}"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Unauthorized' to <see cref="Microsoft.AspNetCore.Http.Result.ForbidResult"/>
	/// </remarks>
	public static void MapForbiddenResponses(this IEndpointRouteBuilder app)
	{
		DomainUnauthorizedService service = new();
		
		var routes = new [] 
			{
				app.MapGet("GetUnauthorizedWithNoMessage", () => service.GetUnauthorizedWithNoMessage().ToResult()),
				app.MapGet("GetUnauthorizedWithMessage", () => service.GetUnauthorizedWithMessage().ToResult()),
				
				app.MapGet("GetUnauthorizedWithNoMessageWhenExpectedNumber", () => service.GetUnauthorizedWithNoMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetUnauthorizedWithMessageWhenExpectedNumber", () => service.GetUnauthorizedWithMessageWhenExpectedNumber().ToResult()),
				
				app.MapGet("GetUnauthorizedWithNoMessageWhenExpectedNumberTuple", () => service.GetUnauthorizedWithNoMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetUnauthorizedWithMessageWhenExpectedNumberTuple", () => service.GetUnauthorizedWithMessageWhenExpectedNumberTuple().ToResult())
			};
		
		foreach (var route in routes)                               
		   route.WithTags("Failed: 403 Forbidden")
				.ProducesProblem(StatusCodes.Status403Forbidden);
	}
}
