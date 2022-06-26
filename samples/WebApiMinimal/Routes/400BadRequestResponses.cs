using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes to return HTTP 400 status
	/// </summary>
	public static void MapBadRequestResponses(this IEndpointRouteBuilder app)
	{
		DomainFailedService service = new();
		
		var routes = new [] 
			{
				app.MapGet("GetErrorWithNoMessage", () => service.GetFailedWithNoMessage().ToResult()),
				app.MapGet("GetErrorWithMessage", () => service.GetFailedWithMessage().ToResult()),
				app.MapGet("GetErrorWithMessages", () => service.GetFailedWithMessages().ToResult()),
				
				app.MapGet("GetErrorWithNoMessageWhenExpectedNumber", () => service.GetFailedWithNoMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetFailedWithMessageWhenExpectedNumber", () => service.GetFailedWithMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetFailedWithMessagesWhenExpectedNumber", () => service.GetFailedWithMessagesWhenExpectedNumber().ToResult()),
				
				app.MapGet("GetFailedWithNoMessageWhenExpectedNumberTuple", () => service.GetFailedWithNoMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetFailedWithMessageWhenExpectedNumberTuple", () => service.GetFailedWithMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetFailedWithMessagesWhenExpectedNumberTuple", () => service.GetFailedWithMessagesWhenExpectedNumberTuple().ToResult())
			};
		
		foreach (var route in routes)                               
		   route.WithTags("Failed: 400 BadRequest")
				.Produces(StatusCodes.Status400BadRequest);
	}
}
