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
	///		Converts <see cref="Task{TResult}"/> of <see cref="IDomainResult{T}"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='ContentTooLarge' to <see cref="Microsoft.AspNetCore.Http.Result.ContentTooLargeObjectResult"/>
	/// </remarks>
	public static void MapContentTooLargeResponses(this IEndpointRouteBuilder app)
	{
		DomainContentTooLargeService service = new();
		
		var routes = new [] 
			{
				app.MapGet("GetContentTooLargeWithNoMessage",	() => service.GetContentTooLargeWithNoMessage().ToResult()),
				app.MapGet("GetContentTooLargeWithMessage",		() => service.GetContentTooLargeWithMessage().ToResult()),
				
				app.MapGet("GetContentTooLargeWithNoMessageWhenExpectedNumber",	() => service.GetContentTooLargeWithNoMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetContentTooLargeWithMessageWhenExpectedNumber",	() => service.GetContentTooLargeWithMessageWhenExpectedNumber().ToResult()),
				
				app.MapGet("GetContentTooLargeWithNoMessageWhenExpectedNumberTuple",() => service.GetContentTooLargeWithNoMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetContentTooLargeWithMessageWhenExpectedNumberTuple",	() => service.GetContentTooLargeWithMessageWhenExpectedNumberTuple().ToResult())
			};
		
		foreach (var route in routes)                               
		   route.WithTags("Failed: 413 ContentTooLarge")
				.ProducesProblem(StatusCodes.Status413PayloadTooLarge);
	}
}
