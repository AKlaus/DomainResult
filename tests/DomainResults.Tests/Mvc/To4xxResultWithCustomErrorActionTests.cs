using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
[Collection("Sequential")]
public class To_4xx_Result_With_Custom_ErrorAction_Tests
{
	private const string CustomType = "https://example.com/errors/validation";
	private const string CustomInstance = "/api/users/123";
	private const string CustomExtensionKey = "traceId";
	private const string CustomExtensionValue = "abc-123-xyz";

	#region Tests for IDomainResult<T> with errorAction

	[Fact]
	public void ErrorAction_Preserves_All_Custom_Properties_Together_For_IResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Unauthorized<int>("Access denied");

		// WHEN convert to IResult with errorAction that sets multiple custom properties
		var res = domainResult.ToResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Instance = CustomInstance;
			problemDetails.Extensions[CustomExtensionKey] = CustomExtensionValue;
			problemDetails.Extensions["errorCode"] = 1234;
			problemDetails.Status = 403; // Change from 401 to 403
			problemDetails.Title = "Forbidden";
			problemDetails.Detail = "You don't have permission";
		});

		// THEN all properties are preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal(CustomInstance, problemDetails.Instance);
		Assert.Equal(CustomExtensionValue, problemDetails.Extensions[CustomExtensionKey]);
		Assert.Equal(1234, problemDetails.Extensions["errorCode"]);
		Assert.Equal(403, problemDetails.Status);
		Assert.Equal("Forbidden", problemDetails.Title);
		Assert.Equal("You don't have permission", problemDetails.Detail);
	}

	[Fact]
	public async Task ErrorAction_Preserves_All_Custom_Properties_Together_For_ActionResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Unauthorized<int>("Access denied");

		// WHEN convert to ActionResult with errorAction that sets multiple custom properties
		var actionRes = domainResult.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Instance = CustomInstance;
			problemDetails.Extensions[CustomExtensionKey] = CustomExtensionValue;
			problemDetails.Extensions["errorCode"] = 1234;
			problemDetails.Status = 403; // Change from 401 to 403
			problemDetails.Title = "Forbidden";
			problemDetails.Detail = "You don't have permission";
		}) as ObjectResult;

		// THEN all properties are preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal(CustomInstance, problemDetails.Instance);
		Assert.Equal(CustomExtensionValue, problemDetails.Extensions[CustomExtensionKey]);
		Assert.Equal(1234, problemDetails.Extensions["errorCode"]);
		Assert.Equal(403, problemDetails.Status);
		Assert.Equal("Forbidden", problemDetails.Title);
		Assert.Equal("You don't have permission", problemDetails.Detail);

		// WHEN the ActionResult runs
		var (statusCode, body) = await actionRes.ExecuteToJsonAsync();

		// THEN the response keeps the properties and the extension of the errorAction
		Assert.Equal(403, statusCode);
		Assert.Equal(403, body.GetProperty("status").GetInt32());
		Assert.Equal(CustomType, body.GetProperty("type").GetString());
		Assert.Equal(CustomInstance, body.GetProperty("instance").GetString());
		Assert.Equal("Forbidden", body.GetProperty("title").GetString());
		Assert.Equal("You don't have permission", body.GetProperty("detail").GetString());
		Assert.Equal(1234, body.GetProperty("errorCode").GetInt32());
		// and the 'traceId' of the ProblemDetailsFactory replaces the one of the errorAction
		var traceId = body.GetProperty(CustomExtensionKey).GetString();
		Assert.False(string.IsNullOrEmpty(traceId));
		Assert.NotEqual(CustomExtensionValue, traceId);
	}

	#endregion

	#region Tests for the other result shapes with errorAction

	[Theory]
	[MemberData(nameof(ResultShapeCases))]
	[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "The shape names the test case")]
	public async Task ErrorAction_Preserves_Custom_Properties_For_Shape_IResult(string shape, Func<Task<IResult>> convert)
	{
		// GIVEN a failed domain result of the shape
		// WHEN convert to IResult with errorAction that sets custom properties
		var res = await convert();

		// THEN custom properties are preserved
		Then_ProblemDetails_Has_Custom_Properties((res as IValueHttpResult)?.Value as ProblemDetails);
	}
	public static readonly TheoryData<string, Func<Task<IResult>>> ResultShapeCases = new()
		{
			{ "Task<IDomainResult<T>>",		() => DomainResult.FailedTask<int>("Error").ToResult((problemDetails, _) => SetCustomProperties(problemDetails)) },
			{ "(T, IDomainResult)",			() => Task.FromResult((10, DomainResult.Failed("Error")).ToResult((problemDetails, _) => SetCustomProperties(problemDetails))) },
			{ "Task<(T, IDomainResult)>",	() => Task.FromResult((10, DomainResult.Failed("Error"))).ToResult((problemDetails, _) => SetCustomProperties(problemDetails)) },
			{ "IDomainResult",				() => Task.FromResult(DomainResult.Failed("Error").ToResult((problemDetails, _) => SetCustomProperties(problemDetails))) },
			{ "Task<IDomainResult>",		() => DomainResult.FailedTask("Error").ToResult((problemDetails, _) => SetCustomProperties(problemDetails)) },
		};

	[Theory]
	[MemberData(nameof(ActionResultShapeCases))]
	[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "The shape names the test case")]
	public async Task ErrorAction_Preserves_Custom_Properties_For_Shape_ActionResult(string shape, Func<Task<IActionResult>> convert)
	{
		// GIVEN a failed domain result of the shape
		// WHEN convert to ActionResult with errorAction that sets custom properties
		var actionRes = await convert() as ObjectResult;

		// THEN custom properties are preserved
		Assert.NotNull(actionRes);
		Then_ProblemDetails_Has_Custom_Properties(actionRes.Value as ProblemDetails);
	}
	public static readonly TheoryData<string, Func<Task<IActionResult>>> ActionResultShapeCases = new()
		{
			{ "Task<IDomainResult<T>>",		() => DomainResult.FailedTask<int>("Error").ToActionResult((problemDetails, _) => SetCustomProperties(problemDetails)) },
			{ "(T, IDomainResult)",			() => Task.FromResult((10, DomainResult.Failed("Error")).ToActionResult((problemDetails, _) => SetCustomProperties(problemDetails))) },
			{ "Task<(T, IDomainResult)>",	() => Task.FromResult((10, DomainResult.Failed("Error"))).ToActionResult((problemDetails, _) => SetCustomProperties(problemDetails)) },
			{ "IDomainResult",				() => Task.FromResult(DomainResult.Failed("Error").ToActionResult((problemDetails, _) => SetCustomProperties(problemDetails))) },
			{ "Task<IDomainResult>",		() => DomainResult.FailedTask("Error").ToActionResult((problemDetails, _) => SetCustomProperties(problemDetails)) },
		};

	#endregion

	#region Tests verifying errorAction receives correct domain state

	[Fact]
	public void ErrorAction_Receives_Correct_DomainResult_State()
	{
		// GIVEN a failed domain result with errors
		var domainResult = DomainResult.Failed<int>(["Error 1", "Error 2"]);

		// WHEN convert with errorAction that inspects the state
		IDomainResult<int>? capturedState = null;
		domainResult.ToResult((_, state) => { capturedState = state; });

		// THEN the state parameter contains the correct domain result
		Assert.NotNull(capturedState);
		Assert.Equal(DomainOperationStatus.Failed, capturedState!.Status);
		Assert.Equal(2, capturedState.Errors.Count());
		Assert.Contains("Error 1", capturedState.Errors);
		Assert.Contains("Error 2", capturedState.Errors);
	}

	#endregion

	#region Auxiliary methods [PRIVATE] -----------------------------------

	private static void SetCustomProperties(ProblemDetails problemDetails)
	{
		problemDetails.Type = CustomType;
		problemDetails.Instance = CustomInstance;
		problemDetails.Extensions["errorCode"] = 1234;
	}

	private static void Then_ProblemDetails_Has_Custom_Properties(ProblemDetails? problemDetails)
	{
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal(CustomInstance, problemDetails.Instance);
		Assert.Equal(1234, problemDetails.Extensions["errorCode"]);
	}

	#endregion // Auxiliary methods [PRIVATE] -----------------------------
}
