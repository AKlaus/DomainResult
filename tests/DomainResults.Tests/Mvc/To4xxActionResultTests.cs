using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[Collection("Sequential")]
public class To_4xx_ActionResult_Tests
{
	[Theory]
	[MemberData(nameof(FailedResultWithValueCases))]
	public void Failed_DomainResult_With_Value<TValue>(IDomainResult<TValue> domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = domainValue.ToActionResult();

		Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}		
	public static readonly IEnumerable<object[]> FailedResultWithValueCases = TestCases(DomainResult.Failed<int>, DomainResult.NotFound<int>, DomainResult.Unauthorized<int>, DomainResult.Conflict<int>, DomainResult.CriticalDependencyError<int>);

	[Theory]
	[MemberData(nameof(FailedResultWithValueTaskCases))]
	public async Task Failed_DomainResult_With_Value_Task<TValue>(Task<IDomainResult<TValue>> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = await domainValueTask.ToActionResult();

		Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedResultWithValueTaskCases = TestCases(DomainResult.FailedTask<int>, DomainResult.NotFoundTask<int>, DomainResult.UnauthorizedTask<int>, DomainResult.ConflictTask<int>, DomainResult.CriticalDependencyErrorTask<int>);

	[Theory]
	[MemberData(nameof(FailedResultCases))]
	public void Failed_DomainResult(IDomainResult domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = domainValue.ToActionResult();

		Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedResultCases = TestCases(DomainResult.Failed, DomainResult.NotFound, DomainResult.Unauthorized, DomainResult.Conflict, DomainResult.CriticalDependencyError);

	[Theory]
	[MemberData(nameof(FailedResultTaskCases))]
	public async Task Failed_DomainResult_Task(Task<IDomainResult> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = await domainValueTask.ToActionResult();

		Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedResultTaskCases = TestCases(DomainResult.FailedTask, DomainResult.NotFoundTask, DomainResult.UnauthorizedTask, DomainResult.ConflictTask, DomainResult.CriticalDependencyErrorTask);

	[Theory]
	[MemberData(nameof(FailedValueTestCases))]
	public void Failed_ValueResult((int, IDomainResult) domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = domainValue.ToActionResult();

		Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedValueTestCases = TestCases(DomainResult.Failed, DomainResult.NotFound, DomainResult.Unauthorized, DomainResult.Conflict, DomainResult.CriticalDependencyError, v => (10, v));

	[Theory]
	[MemberData(nameof(FailedValueTaskTestCases))]
	public async Task Failed_ValueResult_Task(Task<(int, IDomainResult)> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionRes = await domainValueTask.ToActionResult();

		Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
	}
	[Theory]
	[MemberData(nameof(FailedValueTaskTestCases))]
	public void Failed_ValueResult_Task_ActionOfT(Task<(int, IDomainResult)> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		var actionResTask = domainValueTask.ToActionResult();

		Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionResTask.Result, expectedCode, expectedTitle, expectedErrorMsg);
	}
	public static readonly IEnumerable<object[]> FailedValueTaskTestCases = TestCases(DomainResult.Failed, DomainResult.NotFound, DomainResult.Unauthorized, DomainResult.Conflict, DomainResult.CriticalDependencyError, v => Task.FromResult((10, v)));

	#region Auxiliary methods [PRIVATE] -----------------------------------

	/// <summary>
	///		The 'THEN' section of the tests, checking the Response type and the <see cref="ProblemDetails"/>
	/// </summary>
	/// <param name="actionResult"> The <see cref="IActionResult"/> in question </param>
	/// <param name="expectedCode"> The expected HTTP code </param>
	/// <param name="expectedTitle"> The expected title in the <see cref="ProblemDetails"/> </param>
	/// <param name="expectedErrorMsg"> The expected description messages in the <see cref="ProblemDetails"/> </param>
	private void Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(IActionResult actionResult, int expectedCode, string expectedTitle, string expectedErrorMsg)
	{
		// THEN the response type is correct
		var objResult = actionResult as ObjectResult;
		Assert.NotNull(objResult);
		var problemDetails = objResult!.Value as ProblemDetails;
		Assert.NotNull(problemDetails);

		// and the ProblemDetails properties are as expected
		Assert.Equal(expectedCode, problemDetails!.Status);
		Assert.Equal(expectedTitle, problemDetails.Title);
		Assert.Equal(expectedErrorMsg, problemDetails.Detail);
	}

	/// <summary>
	///		Get test cases (input test parameters for the <see cref="MemberDataAttribute"/>)
	/// </summary>
	/// <param name="domainFailedFunc"> The <see cref="DomainOperationStatus.Failed"/> function </param>
	/// <param name="domainNotFoundFunc"> The <see cref="DomainOperationStatus.NotFound"/> function </param>
	/// <param name="domainUnauthFunc"> The <see cref="DomainOperationStatus.Unauthorized"/> function </param>
	/// <param name="domainConflictFunc"> The <see cref="DomainOperationStatus.Conflict"/> function </param>
	/// <param name="domainCriticalFunc"> The <see cref="DomainOperationStatus.CriticalDependencyError"/> function </param>
	/// <param name="wrapInFunc"> Optional wrapper <see cref="ValueTuple"/> of the Domain result method </param>
	/// <returns> Input test parameters </returns>
	private static IEnumerable<object[]> TestCases<T>(
		Func<IEnumerable<string>,T> domainFailedFunc, 
		Func<IEnumerable<string>, T> domainNotFoundFunc, 
		Func<string, T> domainUnauthFunc,  
		Func<string, T> domainConflictFunc,  
		Func<string, T> domainCriticalFunc, 
		Func<T, object>? wrapInFunc = null)
	{
		object OptionalWrapper (T value) => wrapInFunc?.Invoke(value) ?? value!;

		var returnValues = new List<object[]>
			{
				new[] { OptionalWrapper(domainFailedFunc(new[] { "1" })),		ActionResultConventions.FailedHttpCode,	 "Bad Request", "1" },
				new[] { OptionalWrapper(domainFailedFunc(new[] { "1", "2" })),	ActionResultConventions.FailedHttpCode,	 "Bad Request", "1, 2" },
				new[] { OptionalWrapper(domainNotFoundFunc(new[] { "1" })),		ActionResultConventions.NotFoundHttpCode, "Not Found",   "1" },
				new[] { OptionalWrapper(domainNotFoundFunc(new[] { "1", "2" })),ActionResultConventions.NotFoundHttpCode, "Not Found",   "1, 2" },
				new[] { OptionalWrapper(domainUnauthFunc("1")),					ActionResultConventions.UnauthorizedHttpCode, "Unauthorized access",   "1" },
				new[] { OptionalWrapper(domainConflictFunc("1")),				ActionResultConventions.ConflictHttpCode, "Conflict with the current state",   "1" },
				new[] { OptionalWrapper(domainCriticalFunc("1")),				ActionResultConventions.CriticalDependencyErrorHttpCode, "External service unavailable",   "1" },
			};
		foreach (var val in returnValues)
			yield return val;
	}
	#endregion // Auxiliary methods [PRIVATE] -----------------------------
}