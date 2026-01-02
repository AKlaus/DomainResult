using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes returning HTTP 404 status
	/// </summary>
	/// <remarks>
	///		Converts <see cref="Task{TResult}"/> of <see cref="IDomainResult{T}"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='NotFound' to <see cref="Microsoft.AspNetCore.Http.Result.NotFoundObjectResult"/>
	/// </remarks>
	public static void MapNotFoundResponses(this IEndpointRouteBuilder app)
	{
		DomainNotFoundService service = new();
		
		var routes = new [] 
			{
				app.MapGet("GetNotFoundWithNoMessage",	() => service.GetNotFoundWithNoMessage().ToResult()),
				app.MapGet("GetNotFoundWithNoMessageTask",() => service.GetNotFoundWithNoMessageTask().ToResult()),
				app.MapGet("GetNotFoundWithMessage",		() => service.GetNotFoundWithMessage().ToResult()),
				app.MapGet("GetNotFoundWithMessageTask",	() => service.GetNotFoundWithMessageTask().ToResult()),
				app.MapGet("GetNotFoundWithMessages",	() => service.GetNotFoundWithMessages().ToResult()),
				app.MapGet("GetNotFoundWithMessagesTask",() => service.GetNotFoundWithMessagesTask().ToResult()),

				app.MapGet("GetNotFoundWithNoMessageWhenExpectedNumber",		() => service.GetNotFoundWithNoMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetNotFoundWithNoMessageWhenExpectedNumberTask",	() => service.GetNotFoundWithNoMessageWhenExpectedNumberTask().ToResult()),
				app.MapGet("GetNotFoundWithMessageWhenExpectedNumber",		() => service.GetNotFoundWithMessageWhenExpectedNumber().ToResult()),
				app.MapGet("GetNotFoundWithMessageWhenExpectedNumberTask",	() => service.GetNotFoundWithMessageWhenExpectedNumberTask().ToResult()),
				app.MapGet("GetNotFoundWithMessagesWhenExpectedNumber",		() => service.GetNotFoundWithMessagesWhenExpectedNumber().ToResult()),
				app.MapGet("GetNotFoundWithMessagesWhenExpectedNumberTask",	() => service.GetNotFoundWithMessagesWhenExpectedNumberTask().ToResult()),

				app.MapGet("GetNotFoundWithNoMessageWhenExpectedNumberTuple",	() => service.GetNotFoundWithNoMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetNotFoundWithNoMessageWhenExpectedNumberTupleTask",() => service.GetNotFoundWithNoMessageWhenExpectedNumberTupleTask().ToResult()),
				app.MapGet("GetNotFoundWithMessageWhenExpectedNumberTuple",		() => service.GetNotFoundWithMessageWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetNotFoundWithMessageWhenExpectedNumberTupleTask",	() => service.GetNotFoundWithMessageWhenExpectedNumberTupleTask().ToResult()),
				app.MapGet("GetNotFoundWithMessagesWhenExpectedNumberTuple",		() => service.GetNotFoundWithMessagesWhenExpectedNumberTuple().ToResult()),
				app.MapGet("GetNotFoundWithMessagesWhenExpectedNumberTupleTask",	() => service.GetNotFoundWithMessagesWhenExpectedNumberTupleTask().ToResult())
			};
		
		foreach (var route in routes)                               
		   route.WithTags("Failed: 404 NotFound")
				.ProducesProblem(StatusCodes.Status404NotFound);
	}
}
