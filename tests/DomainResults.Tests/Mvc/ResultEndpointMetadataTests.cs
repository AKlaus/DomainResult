using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace DomainResults.Tests.Mvc;

/// <summary>
///		Checks the response metadata that the minimal API reads from the return types of the <see cref="IResult"/> conversions
/// </summary>
public class ResultEndpointMetadataTests
{
	[Theory]
	[MemberData(nameof(ToResultCases))]
	[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "The shape names the test case")]
	public void ToResult_EndpointReturnsConversion_DescribesSuccessResponses(string shape, Delegate handler, string[] expectedResponses)
	{
		// GIVEN a minimal-API endpoint that returns the conversion of the shape
		// WHEN the minimal API builds the endpoint
		var responses = GetResponses(handler);

		// THEN the metadata describes the success responses
		Assert.Superset(expectedResponses.ToHashSet(), responses.ToHashSet());
		// and no problem responses, as they need '.ProducesProblem()'
		Assert.DoesNotContain(responses, IsProblemResponse);
	}
	public static readonly TheoryData<string, Delegate, string[]> ToResultCases = new()
		{
			{ "IDomainResult",				() => DomainResult.Success().ToResult(),						[NoContentResponse] },
			{ "Task<IDomainResult>",		() => DomainResult.SuccessTask().ToResult(),					[NoContentResponse] },
			{ "IDomainResult<T>",			() => DomainResult.Success(10).ToResult(),						[OkIntResponse, NoContentResponse] },
			{ "Task<IDomainResult<T>>",		() => DomainResult.SuccessTask(10).ToResult(),					[OkIntResponse, NoContentResponse] },
			{ "(T, IDomainResult)",			() => (10, IDomainResult.Success()).ToResult(),					[OkIntResponse, NoContentResponse] },
			{ "Task<(T, IDomainResult)>",	() => Task.FromResult((10, IDomainResult.Success())).ToResult(),	[OkIntResponse, NoContentResponse] },
		};

	[Theory]
	[MemberData(nameof(ToCustomResultCases))]
	[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "The shape names the test case")]
	public void ToCustomResult_EndpointReturnsConversion_DescribesCustomResponses(string shape, Delegate handler, string[] expectedResponses)
	{
		// GIVEN a minimal-API endpoint that returns the custom conversion of the shape
		// WHEN the minimal API builds the endpoint
		var responses = GetResponses(handler);

		// THEN the metadata describes the responses of the custom conversion
		Assert.Superset(expectedResponses.ToHashSet(), responses.ToHashSet());
		// and no problem responses, as they need '.ProducesProblem()'
		Assert.DoesNotContain(responses, IsProblemResponse);
	}
	public static readonly TheoryData<string, Delegate, string[]> ToCustomResultCases = new()
		{
			{ "IDomainResult<T>",			() => DomainResult.Success(10).ToCustomResult(ToCreated),						[CreatedIntResponse] },
			{ "Task<IDomainResult<T>>",		() => DomainResult.SuccessTask(10).ToCustomResult(ToCreated),					[CreatedIntResponse] },
			{ "(T, IDomainResult)",			() => (10, IDomainResult.Success()).ToCustomResult(ToCreated),					[CreatedIntResponse] },
			{ "Task<(T, IDomainResult)>",	() => Task.FromResult((10, IDomainResult.Success())).ToCustomResult(ToCreated),	[CreatedIntResponse] },
			// A custom conversion that returns a 'Results<...>' union describes each of its results
			{ "IDomainResult<T?> to a union",
				() => DomainResult.Success<int?>(null).ToCustomResult(ToCreatedOrNoContent),									[CreatedNullableIntResponse, NoContentResponse] },
		};

	private const string OkIntResponse = "200 Int32";
	private const string CreatedIntResponse = "201 Int32";
	private const string CreatedNullableIntResponse = "201 Nullable`1";
	private const string NoContentResponse = "204";

	/// <summary>
	///		Maps the handler to an endpoint, and returns its response metadata as '{HTTP code} {value type}' entries
	/// </summary>
	/// <remarks>
	///		For a 'Task&lt;Results&lt;...&gt;&gt;' return type, .NET 9 also adds a 200 response with the 'Results&lt;...&gt;' type.
	///		So the tests check for a superset.
	/// </remarks>
	private static string[] GetResponses(Delegate handler)
	{
		var app = WebApplication.CreateBuilder().Build();
		app.MapGet("/", handler);
		var endpoint = ((IEndpointRouteBuilder)app).DataSources.Single().Endpoints.Single();
		return endpoint.Metadata
		               .GetOrderedMetadata<IProducesResponseTypeMetadata>()
		               .Select(m => m.Type is null || m.Type == typeof(void) ? $"{m.StatusCode}" : $"{m.StatusCode} {m.Type.Name}")
		               .ToArray();
	}

	private static bool IsProblemResponse(string response) => response[0] is '4' or '5';

	private static Created<int> ToCreated(int value) => TypedResults.Created("/items/1", value);

	private static Results<Created<int?>, NoContent> ToCreatedOrNoContent(int? value)
		=> value is null ? TypedResults.NoContent() : TypedResults.Created("/items/1", value);
}
