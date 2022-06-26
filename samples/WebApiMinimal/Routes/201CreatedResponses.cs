using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes to return HTTP 201 status
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
		   .Produces(StatusCodes.Status201Created, typeof(int));
		
		app.MapPost("Post201CreatedFromTupleTask", 
			   () => service
							.GetSuccessWithNumericValueTupleTask()
							.ToCustomResult(val => Results.CreatedAtRoute("GetById", new { id = val }, val))
					)
		   .WithTags("Success: 201 Created")
		   .Produces(StatusCodes.Status201Created, typeof(int));
		
		app.MapPost("Post201Created", 
			   () => service
							.GetSuccessWithNumericValue()
							.ToCustomResult(val => Results.CreatedAtRoute("GetById", new { id = val }, val))
			   )
		   .WithTags("Success: 201 Created")
		   .Produces(StatusCodes.Status201Created, typeof(int));
				
		app.MapPost("Post201CreatedTask", 
			   () => service
							.GetSuccessWithNumericValueTask()
							.ToCustomResult(val => Results.CreatedAtRoute("GetById", new { id = val }, val))
			   )
		   .WithTags("Success: 201 Created")
		   .Produces(StatusCodes.Status201Created, typeof(int));
		
		app.MapGet("{id}", (int id) => Results.Ok(new { id }))
		   .WithTags("Success: 201 Created")
		   .WithMetadata(new RouteNameMetadata("GetById"))
		   .Produces(StatusCodes.Status200OK, typeof(int));
	}
}
