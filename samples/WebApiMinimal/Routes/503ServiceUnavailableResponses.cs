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
	///		Map all routes returning HTTP 503 status
	/// </summary>
	/// <remarks>
	///		Converts <see cref="Task{TResult}"/> of <see cref="IDomainResult{T}"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='CriticalDependencyError' to HTTP code 503
	/// </remarks>
	public static void MapServiceUnavailableResponses(this IEndpointRouteBuilder app)
	{
		DomainCriticalUnavailableService service = new();
		
		var routes = new [] 
			{
				app.MapGet("GetCriticalDependencyErrorWithNoMessage",() => service.GetCriticalDependencyErrorWithNoMessage().ToResult()),
				app.MapGet("GetCriticalDependencyErrorWithMessage",	() => service.GetCriticalDependencyErrorWithMessage().ToResult()),
				
				app.MapGet("GetCriticalDependencyErrorWithNoMessageWhenExpectedNumber",	() => service.GetCriticalDependencyErrorWithNoMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetCriticalDependencyErrorWithMessageWhenExpectedNumber",	() => service.GetCriticalDependencyErrorWithMessageWhenExpectedNumber().ToResult()),
				
				app.MapGet("GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTuple",() => service.GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetCriticalDependencyErrorWithMessageWhenExpectedNumberTuple",	() => service.GetCriticalDependencyErrorWithMessageWhenExpectedNumberTuple().ToResult())
			};
		
		foreach (var route in routes)                               
		   route.WithTags("Error: 503 ServiceUnavailable")
				.Produces(StatusCodes.Status503ServiceUnavailable);
	}
}
