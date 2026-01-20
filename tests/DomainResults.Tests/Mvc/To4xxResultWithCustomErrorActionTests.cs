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
	public void ErrorAction_Modifies_ProblemDetails_Properties_For_IResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Failed<int>("Validation failed");

		// WHEN convert to IResult with errorAction that modifies properties
		var res = domainResult.ToResult((problemDetails, _) =>
		{
			problemDetails.Status = 422;
			problemDetails.Title = "Custom Title";
			problemDetails.Detail = "Custom Detail";
		});

		// THEN the modified properties are present in the response
		res.AssertObjectResultTypeWithProblemDetails(422, "Custom Title", "Custom Detail");
	}

	[Fact]
	public void ErrorAction_Modifies_ProblemDetails_Properties_For_ActionResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Failed<int>("Validation failed");

		// WHEN convert to ActionResult with errorAction that modifies properties
		var actionRes = domainResult.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Status = 422;
			problemDetails.Title = "Custom Title";
			problemDetails.Detail = "Custom Detail";
		}) as ObjectResult;

		// THEN the modified properties are present in the response
		Assert.NotNull(actionRes);
		var problemDetailsResult = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetailsResult);
		Assert.Equal(422, problemDetailsResult.Status);
		Assert.Equal("Custom Title", problemDetailsResult.Title);
		Assert.Equal("Custom Detail", problemDetailsResult.Detail);
	}

	[Fact]
	public void ErrorAction_Preserves_Custom_Type_Property_For_IResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.NotFound<int>("User not found");

		// WHEN convert to IResult with errorAction that sets Type
		var res = domainResult.ToResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
		});

		// THEN the Type property is preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
	}

	[Fact]
	public void ErrorAction_Preserves_Custom_Type_Property_For_ActionResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.NotFound<int>("User not found");

		// WHEN convert to ActionResult with errorAction that sets Type
		var actionRes = domainResult.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
		}) as ObjectResult;

		// THEN the Type property is preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
	}

	[Fact]
	public void ErrorAction_Preserves_Custom_Instance_Property_For_IResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Conflict<int>("Resource conflict");

		// WHEN convert to IResult with errorAction that sets Instance
		var res = domainResult.ToResult((problemDetails, _) =>
		{
			problemDetails.Instance = CustomInstance;
		});

		// THEN the Instance property is preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomInstance, problemDetails.Instance);
	}

	[Fact]
	public void ErrorAction_Preserves_Custom_Instance_Property_For_ActionResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Conflict<int>("Resource conflict");

		// WHEN convert to ActionResult with errorAction that sets Instance
		var actionRes = domainResult.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Instance = CustomInstance;
		}) as ObjectResult;

		// THEN the Instance property is preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomInstance, problemDetails.Instance);
	}

	[Fact]
	public void ErrorAction_Preserves_Custom_Extensions_For_IResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Failed<int>("Internal error");

		// WHEN convert to IResult with errorAction that adds extensions
		var res = domainResult.ToResult((problemDetails, _) =>
		{
			problemDetails.Extensions[CustomExtensionKey] = CustomExtensionValue;
			problemDetails.Extensions["errorCode"] = 1234;
		});

		// THEN the Extensions are preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomExtensionValue, problemDetails.Extensions[CustomExtensionKey]);
		Assert.Equal(1234, problemDetails.Extensions["errorCode"]);
	}

	[Fact]
	public void ErrorAction_Preserves_Custom_Extensions_For_ActionResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Failed<int>("Internal error");

		// WHEN convert to ActionResult with errorAction that adds extensions
		var actionRes = domainResult.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Extensions[CustomExtensionKey] = CustomExtensionValue;
			problemDetails.Extensions["errorCode"] = 1234;
		}) as ObjectResult;

		// THEN the Extensions are preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomExtensionValue, problemDetails.Extensions[CustomExtensionKey]);
		Assert.Equal(1234, problemDetails.Extensions["errorCode"]);
	}

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
		Assert.Equal(403, problemDetails.Status);
		Assert.Equal("Forbidden", problemDetails.Title);
		Assert.Equal("You don't have permission", problemDetails.Detail);
	}

	[Fact]
	public void ErrorAction_Preserves_All_Custom_Properties_Together_For_ActionResult()
	{
		// GIVEN a failed domain result
		var domainResult = DomainResult.Unauthorized<int>("Access denied");

		// WHEN convert to ActionResult with errorAction that sets multiple custom properties
		var actionRes = domainResult.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Instance = CustomInstance;
			problemDetails.Extensions[CustomExtensionKey] = CustomExtensionValue;
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
		Assert.Equal(403, problemDetails.Status);
		Assert.Equal("Forbidden", problemDetails.Title);
		Assert.Equal("You don't have permission", problemDetails.Detail);
	}

	#endregion

	#region Tests for Task<IDomainResult<T>> with errorAction

	[Fact]
	public async Task ErrorAction_Preserves_Custom_Properties_For_Task_IResult()
	{
		// GIVEN a failed domain result task
		var domainResultTask = DomainResult.FailedTask<int>("Async error");

		// WHEN convert to IResult with errorAction
		var res = await domainResultTask.ToResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Extensions[CustomExtensionKey] = CustomExtensionValue;
		});

		// THEN custom properties are preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal(CustomExtensionValue, problemDetails.Extensions[CustomExtensionKey]);
	}

	[Fact]
	public async Task ErrorAction_Preserves_Custom_Properties_For_Task_ActionResult()
	{
		// GIVEN a failed domain result task
		var domainResultTask = DomainResult.FailedTask<int>("Async error");

		// WHEN convert to ActionResult with errorAction
		var actionRes = await domainResultTask.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Extensions[CustomExtensionKey] = CustomExtensionValue;
		}) as ObjectResult;

		// THEN custom properties are preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal(CustomExtensionValue, problemDetails.Extensions[CustomExtensionKey]);
	}

	#endregion

	#region Tests for (Value, IDomainResult) tuples with errorAction

	[Fact]
	public void ErrorAction_Preserves_Custom_Properties_For_Tuple_IResult()
	{
		// GIVEN a failed tuple result
		var tupleResult = (10, DomainResult.Failed("Tuple error"));

		// WHEN convert to IResult with errorAction
		var res = tupleResult.ToResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Instance = CustomInstance;
		});

		// THEN custom properties are preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal(CustomInstance, problemDetails.Instance);
	}

	[Fact]
	public void ErrorAction_Preserves_Custom_Properties_For_Tuple_ActionResult()
	{
		// GIVEN a failed tuple result
		var tupleResult = (10, DomainResult.Failed("Tuple error"));

		// WHEN convert to ActionResult with errorAction
		var actionRes = tupleResult.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Instance = CustomInstance;
		}) as ObjectResult;

		// THEN custom properties are preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal(CustomInstance, problemDetails.Instance);
	}

	[Fact]
	public async Task ErrorAction_Preserves_Custom_Properties_For_Tuple_Task_IResult()
	{
		// GIVEN a failed tuple result task
		var tupleResultTask = Task.FromResult((10, DomainResult.Failed("Async tuple error")));

		// WHEN convert to IResult with errorAction
		var res = await tupleResultTask.ToResult((problemDetails, _) =>
		{
			problemDetails.Extensions["requestId"] = "req-456";
		});

		// THEN custom extensions are preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal("req-456", problemDetails.Extensions["requestId"]);
	}

	[Fact]
	public async Task ErrorAction_Preserves_Custom_Properties_For_Tuple_Task_ActionResult()
	{
		// GIVEN a failed tuple result task
		var tupleResultTask = Task.FromResult((10, DomainResult.Failed("Async tuple error")));

		// WHEN convert to ActionResult with errorAction
		var actionRes = await tupleResultTask.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Extensions["requestId"] = "req-456";
		}) as ObjectResult;

		// THEN custom extensions are preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal("req-456", problemDetails.Extensions["requestId"]);
	}

	#endregion

	#region Tests for IDomainResult (no value) with errorAction

	[Fact]
	public void ErrorAction_Preserves_Custom_Properties_For_IDomainResult_IResult()
	{
		// GIVEN a failed domain result (no value)
		var domainResult = DomainResult.NotFound("Resource not found");

		// WHEN convert to IResult with errorAction
		var res = domainResult.ToResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Extensions["resourceType"] = "User";
		});

		// THEN custom properties are preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal("User", problemDetails.Extensions["resourceType"]);
	}

	[Fact]
	public void ErrorAction_Preserves_Custom_Properties_For_IDomainResult_ActionResult()
	{
		// GIVEN a failed domain result (no value)
		var domainResult = DomainResult.NotFound("Resource not found");

		// WHEN convert to ActionResult with errorAction
		var actionRes = domainResult.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Type = CustomType;
			problemDetails.Extensions["resourceType"] = "User";
		}) as ObjectResult;

		// THEN custom properties are preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomType, problemDetails.Type);
		Assert.Equal("User", problemDetails.Extensions["resourceType"]);
	}

	[Fact]
	public async Task ErrorAction_Preserves_Custom_Properties_For_IDomainResult_Task_IResult()
	{
		// GIVEN a failed domain result task (no value)
		var domainResultTask = DomainResult.ConflictTask("Conflict detected");

		// WHEN convert to IResult with errorAction
		var res = await domainResultTask.ToResult((problemDetails, _) =>
		{
			problemDetails.Instance = CustomInstance;
			problemDetails.Extensions["conflictType"] = "DuplicateKey";
		});

		// THEN custom properties are preserved
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomInstance, problemDetails.Instance);
		Assert.Equal("DuplicateKey", problemDetails.Extensions["conflictType"]);
	}

	[Fact]
	public async Task ErrorAction_Preserves_Custom_Properties_For_IDomainResult_Task_ActionResult()
	{
		// GIVEN a failed domain result task (no value)
		var domainResultTask = DomainResult.ConflictTask("Conflict detected");

		// WHEN convert to ActionResult with errorAction
		var actionRes = await domainResultTask.ToActionResult((problemDetails, _) =>
		{
			problemDetails.Instance = CustomInstance;
			problemDetails.Extensions["conflictType"] = "DuplicateKey";
		}) as ObjectResult;

		// THEN custom properties are preserved
		Assert.NotNull(actionRes);
		var problemDetails = actionRes.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(CustomInstance, problemDetails.Instance);
		Assert.Equal("DuplicateKey", problemDetails.Extensions["conflictType"]);
	}

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

	[Fact]
	public void ErrorAction_Can_Conditionally_Modify_Based_On_Errors()
	{
		// GIVEN two domain results - one with errors, one without
		var domainResultWithErrors = DomainResult.Failed<int>(["Error 1"]);
		var domainResultNoErrors = DomainResult.Failed<int>();

		// WHEN convert with conditional errorAction
		var resWithErrors = domainResultWithErrors.ToResult((problemDetails, state) =>
		{
			problemDetails.Title = state.Errors.Count != 0 ? "Has Errors" : "No Errors";
		});

		var resNoErrors = domainResultNoErrors.ToResult((problemDetails, state) =>
		{
			problemDetails.Title = state.Errors.Count != 0 ? "Has Errors" : "No Errors";
		});

		// THEN titles are set correctly based on error presence
		var problemDetailsWithErrors = (resWithErrors as IValueHttpResult)?.Value as ProblemDetails;
		var problemDetailsNoErrors = (resNoErrors as IValueHttpResult)?.Value as ProblemDetails;

		Assert.Equal("Has Errors", problemDetailsWithErrors?.Title);
		Assert.Equal("No Errors", problemDetailsNoErrors?.Title);
	}

	#endregion
}
