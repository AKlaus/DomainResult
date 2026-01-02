using DomainResults.Examples.Domain;
using DomainResults.Mvc;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes returning HTTP 204 status
	/// </summary>
	public static void MapSuccessNoContentResponses(this IEndpointRouteBuilder app)
	{
		DomainSuccessService service = new();
		
		app.MapGet("Get204NoContent", () => service.GetSuccess().ToResult())
		   .WithTags("Success: 204 NoContent")
		   .Produces(StatusCodes.Status204NoContent);
		
		app.MapGet("Get204NoContentTask", () => service.GetSuccessTask().ToResult())
		   .WithTags("Success: 204 NoContent")
		   .Produces(StatusCodes.Status204NoContent);
	}
}
