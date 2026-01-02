using System.Net.Mime;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

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
			                .ToCustomResult(val => Results.CreatedAtRoute("GetById", new { id = val }, val))
				   )
		   .WithTags("Success: 201 Created")
		   .Produces<int>(StatusCodes.Status201Created);
		
		app.MapPost("Post201CreatedFromTupleTask", 
			   () => service
							.GetSuccessWithNumericValueTupleTask()
							.ToCustomResult(val => Results.CreatedAtRoute("GetById", new { id = val }, val))
					)
		   .WithTags("Success: 201 Created")
		   .Produces<int>(StatusCodes.Status201Created);
		
		app.MapPost("Post201Created", 
			   () => service
							.GetSuccessWithNumericValue()
							.ToCustomResult(val => Results.CreatedAtRoute("GetById", new { id = val }, val))
			   )
		   .WithTags("Success: 201 Created")
		   .Produces<int>(StatusCodes.Status201Created);
				
		app.MapPost("Post201CreatedTask", 
			   () => service
							.GetSuccessWithNumericValueTask()
							.ToCustomResult(val => Results.CreatedAtRoute("GetById", new { id = val }, val))
			   )
		   .WithTags("Success: 201 Created")
		   .Produces<int>(StatusCodes.Status201Created);
		
		app.MapGet("{id}", (int id) => Results.Ok($"Sample properties of #{id} record"))
		   .WithTags("Success: 201 Created")
		   .WithName("GetById")
		   .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain);
	}
}
