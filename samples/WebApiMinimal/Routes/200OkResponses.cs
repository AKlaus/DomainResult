using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes returning HTTP 200 status
	/// </summary>
	public static void MapSuccessResponses(this IEndpointRouteBuilder app)
	{
		DomainSuccessService service = new();
		
		app.MapGet("200OkWithNumber", () => service.GetSuccessWithNumericValue().ToResult())
		   .WithTags("Success: 200 Ok")
		   .Produces<int>();
		
		app.MapGet("Get200OkWithNumberTask", () => service.GetSuccessWithNumericValueTask().ToResult())
		   .WithTags("Success: 200 Ok")
		   .Produces<int>();
		
		app.MapGet("Get200OkTupleWithNumber", () => service.GetSuccessWithNumericValueTuple().ToResult())
		   .WithTags("Success: 200 Ok")
		   .Produces<int>();
				
		app.MapGet("Get200OkTupleWithNumberTask", () => service.GetSuccessWithNumericValueTupleTask().ToResult())
		   .WithTags("Success: 200 Ok")
		   .Produces<int>();
	}
}
