using System.Linq;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes to return HTTP 400 status with custom error message
	/// </summary>
	public static void MapBadRequestCustomErrorResponses(this IEndpointRouteBuilder app)
	{
		DomainFailedService service = new();
		
		var routes = new [] 
			{
				app.MapGet("GetErrorWithCustomStatusAndMessage", 
					   () => service.GetFailedWithNoMessage()
											.ToResult((problemDetails, state) =>
											{
												if (state.Errors.Any())
													return;
												problemDetails.Status = 422;
												problemDetails.Title = "D'oh!";
												problemDetails.Detail = "I wish devs put more efforts into it...";
											}))
				   .Produces(StatusCodes.Status422UnprocessableEntity),
				
				app.MapGet("GetErrorWithCustomTitleAndOriginalMessage",
					   () => service.GetFailedWithMessageTask()
											.ToResult((problemDetails, _) => { problemDetails.Title = "D'oh!"; })
						  )
				   .Produces(StatusCodes.Status400BadRequest),
				
				app.MapGet("GetErrorWithCustomStatusAndMessageWhenExpectedNumber", 
					() => service.GetFailedWithNoMessageWhenExpectedNumber()
										.ToResult((problemDetails, state) =>
										{
											if (state.Errors.Any())
												return;
											problemDetails.Status = 422;
											problemDetails.Title = "D'oh!";
											problemDetails.Detail = "I wish devs put more efforts into it...";
										})
						  )
				   .Produces(StatusCodes.Status422UnprocessableEntity),
				
				app.MapGet("GetErrorWithCustomTitleAndOriginalMessageWhenExpectedNumber", 
					() => service.GetFailedWithMessageWhenExpectedNumberTask()
										.ToResult((problemDetails, _) => { problemDetails.Title = "D'oh!"; })
						  )
				   .Produces(StatusCodes.Status400BadRequest),
				
				app.MapGet("GetErrorWithCustomStatusAndMessageWhenExpectedNumberAsTuple", 
					() => service.GetFailedWithNoMessageWhenExpectedNumberTuple()
										.ToResult((problemDetails, state) =>
										{
											if (state.Errors.Any())
												return;
											problemDetails.Status = 422;
											problemDetails.Title = "D'oh!";
											problemDetails.Detail = "I wish devs put more efforts into it...";
										})
						  )
				   .Produces(StatusCodes.Status422UnprocessableEntity),
				
				app.MapGet("GetErrorWithCustomTitleAndOriginalMessageWhenExpectedNumberAsTuple", 
					() => service.GetFailedWithMessageWhenExpectedNumberTupleTask()
										.ToResult((problemDetails, _) => { problemDetails.Title = "D'oh!"; })
						  )
				   .Produces(StatusCodes.Status400BadRequest),
				
				app.MapGet("GetErrorOfTWithCustomTitleAndOriginalMessageWhenExpectedNumberAsTuple", 
					() => service.GetFailedWithMessageWhenExpectedNumberTupleTask()
										.ToResult((problemDetails, _) => { problemDetails.Title = "D'oh!"; })
						  )
				   .Produces(StatusCodes.Status400BadRequest)
			};
		
		foreach (var route in routes)                               
		   route.WithTags("Failed: 400/422 BadRequest with custom message");
	}
}
