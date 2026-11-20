using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DomainResults.Tests.Mvc;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
[Collection("Sequential")]
public class To_4xx_ActionResult_Tests
{
	[Theory]
	[MemberData(nameof(FailedResultWithValueCases))]
	public async Task Failed_DomainResult_With_Value<TValue>(IDomainResult<TValue> domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = domainValue.ToActionResult();
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
		
		var res = domainValue.ToResult();
		Then_Response_Is_IResult_Type_And_ProblemDetails_StatusAndText_Correct(res, expectedCode, expectedTitle, expectedErrorMsg);
	}		
	public static readonly IEnumerable<object[]> FailedResultWithValueCases = TestCases(DomainResult.Failed<int>, DomainResult.NotFound<int>, DomainResult.Unauthorized<int>, DomainResult.Conflict<int>, DomainResult.ContentTooLarge<int>, DomainResult.CriticalDependencyError<int>);

	[Theory]
	[MemberData(nameof(FailedResultWithValueTaskCases))]
	public async Task Failed_DomainResult_With_Value_Task<TValue>(Task<IDomainResult<TValue>> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = await domainValueTask.ToActionResult();
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);

		var res = await domainValueTask.ToResult();
		Then_Response_Is_IResult_Type_And_ProblemDetails_StatusAndText_Correct(res, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedResultWithValueTaskCases = TestCases(DomainResult.FailedTask<int>, DomainResult.NotFoundTask<int>, DomainResult.UnauthorizedTask<int>, DomainResult.ConflictTask<int>, DomainResult.ContentTooLargeTask<int>, DomainResult.CriticalDependencyErrorTask<int>);

	[Theory]
	[MemberData(nameof(FailedResultCases))]
	public async Task Failed_DomainResult(IDomainResult domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = domainValue.ToActionResult();
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);

		var res = domainValue.ToResult();
		Then_Response_Is_IResult_Type_And_ProblemDetails_StatusAndText_Correct(res, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedResultCases = TestCases(DomainResult.Failed, DomainResult.NotFound, DomainResult.Unauthorized, DomainResult.Conflict, DomainResult.ContentTooLarge, DomainResult.CriticalDependencyError);

	[Theory]
	[MemberData(nameof(FailedResultTaskCases))]
	public async Task Failed_DomainResult_Task(Task<IDomainResult> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = await domainValueTask.ToActionResult();
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);

		var res = await domainValueTask.ToResult();
		Then_Response_Is_IResult_Type_And_ProblemDetails_StatusAndText_Correct(res, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedResultTaskCases = TestCases(DomainResult.FailedTask, DomainResult.NotFoundTask, DomainResult.UnauthorizedTask, DomainResult.ConflictTask, DomainResult.ContentTooLargeTask, DomainResult.CriticalDependencyErrorTask);

	[Theory]
	[MemberData(nameof(FailedValueTestCases))]
	public async Task Failed_ValueResult((int, IDomainResult) domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = domainValue.ToActionResult();
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);

		var res = domainValue.ToResult();
		Then_Response_Is_IResult_Type_And_ProblemDetails_StatusAndText_Correct(res, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedValueTestCases = TestCases(DomainResult.Failed, DomainResult.NotFound, DomainResult.Unauthorized, DomainResult.Conflict, DomainResult.ContentTooLarge, DomainResult.CriticalDependencyError, v => (10, v));

	[Theory]
	[MemberData(nameof(FailedValueTaskTestCases))]
	public async Task Failed_ValueResult_Task(Task<(int, IDomainResult)> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = await domainValueTask.ToActionResult();
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);

		var res = await domainValueTask.ToResult();
		Then_Response_Is_IResult_Type_And_ProblemDetails_StatusAndText_Correct(res, expectedCode, expectedTitle, expectedErrorMsg);
	}
	[Theory]
	[MemberData(nameof(FailedValueTaskTestCases))]
	public async Task Failed_ValueResult_Task_ActionOfT(Task<(int, IDomainResult)> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionResTask = domainValueTask.ToActionResult();
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionResTask.Result, expectedCode, expectedTitle, expectedErrorMsg);

		var resTask = domainValueTask.ToResult();
		Then_Response_Is_IResult_Type_And_ProblemDetails_StatusAndText_Correct(resTask.Result, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedValueTaskTestCases = TestCases(DomainResult.Failed, DomainResult.NotFound, DomainResult.Unauthorized, DomainResult.Conflict, DomainResult.ContentTooLarge, DomainResult.CriticalDependencyError, v => Task.FromResult((10, v)));

	[Theory]
	[MemberData(nameof(FailedResultWithValueCases))]
	public async Task Failed_DomainResult_With_Value_ActionResultOfT<TValue>(IDomainResult<TValue> domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = ToActionResult(domainValue.ToActionResultOfT());
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}

	[Theory]
	[MemberData(nameof(FailedValueTestCases))]
	public async Task Failed_ValueResult_ActionResultOfT((int, IDomainResult) domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = ToActionResult(domainValue.ToActionResultOfT());
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}

	[Theory]
	[MemberData(nameof(FailedResultWithValueCases))]
	public async Task Failed_DomainResult_With_Value_CustomActionResult<TValue>(IDomainResult<TValue> domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = domainValue.ToCustomActionResult(v => new CreatedResult("/", v));
		await Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}

	[Fact]
	public async Task NotFound_DomainResult_Type_Is_Rfc_Link()
	{
		// GIVEN a domain result with the 'NotFound' status
		var domainResult = DomainResult.NotFound("1");

		// WHEN the IActionResult and the IResult conversions run
		var (_, actionBody) = await domainResult.ToActionResult().ExecuteToJsonAsync();
		var (_, resultBody) = await domainResult.ToResult().ExecuteToJsonAsync();

		// THEN both conversions write the RFC link of the 404 code as 'type'
		Assert.Equal(ExpectedNotFoundType, actionBody.GetProperty("type").GetString());
		Assert.Equal(ExpectedNotFoundType, resultBody.GetProperty("type").GetString());
	}

	[Fact]
	public async Task Failed_DomainResult_With_ProblemDetails_Customisation()
	{
		// GIVEN 'AddProblemDetails()' with a customisation that adds an extension
		var domainResult = DomainResult.NotFound("1");

		// WHEN the IActionResult conversion runs
		var (_, body) = await domainResult.ToActionResult()
		                                  .ExecuteToJsonAsync(services => services.AddProblemDetails(o => o.CustomizeProblemDetails = c => c.ProblemDetails.Extensions["customised"] = true));

		// THEN the body has the extension of the customisation
		Assert.True(body.GetProperty("customised").GetBoolean());
	}

	#region Auxiliary methods [PRIVATE] -----------------------------------

#if NET8_0_OR_GREATER
	private const string ExpectedNotFoundType = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
#else
	private const string ExpectedNotFoundType = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
#endif

	/// <summary>
	///		The 'THEN' section of the tests, checking the Response type (<see cref="ActionResult"/>), the nested <see cref="ProblemDetails"/> and the response body
	/// </summary>
	/// <param name="actionResult"> The <see cref="IActionResult"/> in question </param>
	/// <param name="expectedCode"> The expected HTTP code </param>
	/// <param name="expectedTitle"> The expected title in the <see cref="ProblemDetails"/> </param>
	/// <param name="expectedErrorMsg"> The expected description messages in the <see cref="ProblemDetails"/> </param>
	private static async Task Then_Response_Is_ActionResult_Type_And_ProblemDetails_StatusAndText_Correct(IActionResult actionResult, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		// THEN the response type is correct
		var objResult = actionResult as ObjectResult;
		Assert.NotNull(objResult);

		// and the ProblemDetails properties are as expected
		var problemDetails = objResult!.Value as ProblemDetails;
		Assert.NotNull(problemDetails);
		Assert.Equal(expectedCode, problemDetails!.Status);
		Assert.Equal(expectedTitle, problemDetails.Title);
		Assert.Equal(expectedErrorMsg, problemDetails.Detail);

		// and the response body has the same values
		var (statusCode, body) = await actionResult.ExecuteToJsonAsync();
		Assert.Equal(expectedCode, statusCode);
		Assert.Equal(expectedCode, body.GetProperty("status").GetInt32());
		Assert.Equal(expectedTitle, body.GetProperty("title").GetString());
		Assert.Equal(expectedErrorMsg, body.GetProperty("detail").GetString());
		// and the 'type' of the IResult conversion
		Assert.Equal(GetIResultType(expectedCode), body.TryGetProperty("type", out var type) ? type.GetString() : null);
		// and a 'traceId', as 'ControllerBase.Problem()' writes
		Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
	}

	/// <summary>
	///		The 'THEN' section of the tests, checking the Response type (<see cref="IResult"/>) and the nested <see cref="ProblemDetails"/>
	/// </summary>
	/// <param name="res"> The <see cref="IResult"/> in question </param>
	/// <param name="expectedCode"> The expected HTTP code </param>
	/// <param name="expectedTitle"> The expected title in the <see cref="ProblemDetails"/> </param>
	/// <param name="expectedErrorMsg"> The expected description messages in the <see cref="ProblemDetails"/> </param>
	private void Then_Response_Is_IResult_Type_And_ProblemDetails_StatusAndText_Correct(IResult res, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		// THEN the response type is correct and the ProblemDetails properties are as expected
		res.AssertObjectResultTypeWithProblemDetails(expectedCode, expectedTitle, expectedErrorMsg);
		// and the 'type' is the default for the HTTP code
		Assert.Equal(GetIResultType(expectedCode), ((res as IValueHttpResult)?.Value as ProblemDetails)?.Type);
	}

	/// <summary>
	///		Gets the 'type' that ASP.NET Core sets for a <see cref="ProblemDetails"/> of the HTTP code in <c>TypedResults.Problem()</c>
	/// </summary>
	private static string? GetIResultType(int statusCode) => TypedResults.Problem(statusCode: statusCode).ProblemDetails.Type;

	private static IActionResult ToActionResult<T>(ActionResult<T> actionResult) => ((IConvertToActionResult)actionResult).Convert();

	/// <summary>
	///		Get test cases (input test parameters for the <see cref="MemberDataAttribute"/>)
	/// </summary>
	/// <param name="domainFailedFunc"> The <see cref="DomainOperationStatus.Failed"/> function </param>
	/// <param name="domainNotFoundFunc"> The <see cref="DomainOperationStatus.NotFound"/> function </param>
	/// <param name="domainUnauthFunc"> The <see cref="DomainOperationStatus.Unauthorized"/> function </param>
	/// <param name="domainConflictFunc"> The <see cref="DomainOperationStatus.Conflict"/> function </param>
	/// <param name="domainContentTooLargeFunc"> The <see cref="DomainOperationStatus.ContentTooLarge"/> function </param>
	/// <param name="domainCriticalFunc"> The <see cref="DomainOperationStatus.CriticalDependencyError"/> function </param>
	/// <param name="wrapInFunc"> Optional wrapper <see cref="ValueTuple"/> of the Domain result method </param>
	/// <returns> Input test parameters </returns>
	private static IEnumerable<object[]> TestCases<T>(
		Func<IEnumerable<string>,T> domainFailedFunc, 
		Func<IEnumerable<string>, T> domainNotFoundFunc, 
		Func<string, T> domainUnauthFunc,  
		Func<string, T> domainConflictFunc,  
		Func<string, T> domainContentTooLargeFunc,  
		Func<string, T> domainCriticalFunc, 
		Func<T, object>? wrapInFunc = null)
	{
		object OptionalWrapper (T value) => wrapInFunc?.Invoke(value) ?? value!;

		var returnValues = new List<object[]>
			{
				new[] { OptionalWrapper(domainFailedFunc(new[] { "1" })),		HttpCodeConvention.FailedHttpCode,	 "Bad Request", "1" },
				new[] { OptionalWrapper(domainFailedFunc(new[] { "1", "2" })),	HttpCodeConvention.FailedHttpCode,	 "Bad Request", "1, 2" },
				new[] { OptionalWrapper(domainNotFoundFunc(new[] { "1" })),		HttpCodeConvention.NotFoundHttpCode, "Not Found",   "1" },
				new[] { OptionalWrapper(domainNotFoundFunc(new[] { "1", "2" })),HttpCodeConvention.NotFoundHttpCode, "Not Found",   "1, 2" },
				new[] { OptionalWrapper(domainUnauthFunc("1")),					HttpCodeConvention.UnauthorizedHttpCode, "Unauthorized access",   "1" },
				new[] { OptionalWrapper(domainConflictFunc("1")),				HttpCodeConvention.ConflictHttpCode, "Conflict with the current state",   "1" },
				new[] { OptionalWrapper(domainContentTooLargeFunc("1")),		HttpCodeConvention.ContentTooLargeHttpCode, "The requested entity is larger than limits defined by server",   "1" },
				new[] { OptionalWrapper(domainCriticalFunc("1")),				HttpCodeConvention.CriticalDependencyErrorHttpCode, "External service unavailable",   "1" },
			};
		foreach (var val in returnValues)
			yield return val;
	}
	#endregion // Auxiliary methods [PRIVATE] -----------------------------
}