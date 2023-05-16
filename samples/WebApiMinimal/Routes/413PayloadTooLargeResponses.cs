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
	///		Map all routes returning HTTP 413 status
	/// </summary>
	/// <remarks>
	///		Converts <see cref="Task{TResult}"/> of <see cref="IDomainResult{T}"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='PayloadTooLarge' to <see cref="Microsoft.AspNetCore.Http.Result.PayloadTooLargeObjectResult"/>
	/// </remarks>
	public static void MapPayloadTooLargeResponses(this IEndpointRouteBuilder app)
	{
		DomainPayloadTooLargeService service = new();
		
		var routes = new [] 
			{
				app.MapGet("GetPayloadTooLargeWithNoMessage",	() => service.GetPayloadTooLargeWithNoMessage().ToResult()),
				app.MapGet("GetPayloadTooLargeWithMessage",		() => service.GetPayloadTooLargeWithMessage().ToResult()),
				
				app.MapGet("GetPayloadTooLargeWithNoMessageWhenExpectedNumber",	() => service.GetPayloadTooLargeWithNoMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetPayloadTooLargeWithMessageWhenExpectedNumber",	() => service.GetPayloadTooLargeWithMessageWhenExpectedNumber().ToResult()),
				
				app.MapGet("GetPayloadTooLargeWithNoMessageWhenExpectedNumberTuple",() => service.GetPayloadTooLargeWithNoMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetPayloadTooLargeWithMessageWhenExpectedNumberTuple",	() => service.GetPayloadTooLargeWithMessageWhenExpectedNumberTuple().ToResult())
			};
		
		foreach (var route in routes)                               
		   route.WithTags("Failed: 413 PayloadTooLarge")
				.ProducesProblem(StatusCodes.Status413PayloadTooLarge);
	}
}
