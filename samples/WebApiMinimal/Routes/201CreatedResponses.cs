using System.Net.Mime;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http.HttpResults;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes returning HTTP 201 status
	/// </summary>
	public static void MapSuccessCreatedResponses(this IEndpointRouteBuilder app)
	{
		DomainSuccessService service = new();
		
		app.MapPost("Post201CreatedFromTuple",
			   () => service
							.GetSuccessWithNumericValueTuple()
			                .ToCustomResult(val => TypedResults.CreatedAtRoute(val, "GetById", new { id = val }))
				   )
		   .WithTags("Success: 201 Created");

		app.MapPost("Post201CreatedFromTupleTask",
			   () => service
							.GetSuccessWithNumericValueTupleTask()
							.ToCustomResult(val => TypedResults.CreatedAtRoute(val, "GetById", new { id = val }))
					)
		   .WithTags("Success: 201 Created");

		app.MapPost("Post201Created",
			   () => service
							.GetSuccessWithNumericValue()
							.ToCustomResult(val => TypedResults.CreatedAtRoute(val, "GetById", new { id = val }))
			   )
		   .WithTags("Success: 201 Created");

		app.MapPost("Post201CreatedTask",
			   () => service
							.GetSuccessWithNumericValueTask()
							.ToCustomResult(val => TypedResults.CreatedAtRoute(val, "GetById", new { id = val }))
			   )
		   .WithTags("Success: 201 Created");

		app.MapPost("Post201CreatedOrNoContentForNullNumber",
			   () => service
							.GetSuccessWithNullNumericValue()
							// The custom conversion receives a null value too.
							// A 'Results<...>' union gives OpenAPI both responses.
							.ToCustomResult(val => val is null
							                          ? (Results<CreatedAtRoute<int?>, NoContent>)TypedResults.NoContent()
							                          : TypedResults.CreatedAtRoute(val, "GetById", new { id = val }))
			   )
		   .WithTags("Success: 201 Created");

		app.MapGet("{id}", (int id) => TypedResults.Ok($"Sample properties of #{id} record"))
		   .WithTags("Success: 201 Created")
		   .WithName("GetById")
		   .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain);
	}
}
