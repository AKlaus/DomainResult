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
	///		Map all routes returning HTTP 400 status
	/// </summary>
	/// <remarks>
	///		Converts <see cref="Task{TResult}"/> of <see cref="IDomainResult{T}"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Failed' to <see cref="Microsoft.AspNetCore.Http.Result.BadRequestObjectResult"/>
	/// </remarks>
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
				.ProducesProblem(StatusCodes.Status400BadRequest);
	}
}
