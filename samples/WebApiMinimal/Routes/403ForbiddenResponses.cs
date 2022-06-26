using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes to return HTTP 403 status
	/// </summary>
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
				.Produces(StatusCodes.Status403Forbidden);
	}
}
