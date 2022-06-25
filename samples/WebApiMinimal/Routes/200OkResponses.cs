using System;
using System.Threading.Tasks;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Examples.WebApiMinimal.Routes;

internal static partial class RoutesExtension 
{
	/// <summary>
	///		Map all routes to return HTTP 200 status
	/// </summary>
	public static void MapSuccessResponses(this IEndpointRouteBuilder app)
	{
		DomainSuccessService service = new();
		
		app.MapGet("200OkWithNumber", (Func<IResult>)(() => service.GetSuccessWithNumericValue().ToResult()))
		   .WithTags("SuccessResponses")
		   .Produces(StatusCodes.Status200OK, typeof(int));
		
		app.MapGet("Get200OkWithNumberTask", (Func<Task<IResult>>)(() => service.GetSuccessWithNumericValueTask().ToResult()))
		   .WithTags("SuccessResponses")
		   .Produces(StatusCodes.Status200OK, typeof(int));
		
		app.MapGet("Get200OkTupleWithNumber", (Func<IResult>)(() => service.GetSuccessWithNumericValueTuple().ToResult()))
		   .WithTags("SuccessResponses")
		   .Produces(StatusCodes.Status200OK, typeof(int));
				
		app.MapGet("Get200OkTupleWithNumberTask", (Func<Task<IResult>>)(() => service.GetSuccessWithNumericValueTupleTask().ToResult()))
		   .WithTags("SuccessResponses")
		   .Produces(StatusCodes.Status200OK, typeof(int));

	}
}
