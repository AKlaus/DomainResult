using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class To_Custom_ActionResult_Success_Tests
{
	[Theory]
	[MemberData(nameof(DomainResultTestCases))]
	public void DomainResult_Converted_To_CreatedResult_Test<TValue>(IDomainResult<TValue> domainValue, Func<IDomainResult<TValue>, TValue> getValueFunc, Uri urlUri)
	{
		var actionResult = domainValue.ToCustomActionResult(val => new CreatedResult(urlUri, val));

		Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct(actionResult, getValueFunc(domainValue));

#if NET6_0_OR_GREATER
		var res = domainValue.ToCustomResult(val => Results.Created(urlUri, val));
		Then_Response_Is_IResult_Type_And_Value_And_Url_Are_Correct(res, getValueFunc(domainValue));
#endif
	}
	
	[Theory]
	[MemberData(nameof(DomainResultTestCases))]
	public void DomainResult_Converted_To_CreatedResultOfT_Test<TValue>(IDomainResult<TValue> domainValue, Func<IDomainResult<TValue>, TValue> getValueFunc, Uri urlUri)
	{
		var actionResult = domainValue.ToCustomActionResultOfT(val => new CreatedResult(urlUri, val));

		Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct(actionResult, getValueFunc(domainValue));
	}
	public static readonly IEnumerable<object[]> DomainResultTestCases = GetDomainResultTestCases(false);
	
	[Theory]
	[MemberData(nameof(DomainResultTaskTestCases))]
	public async Task DomainResult_Task_Converted_To_CreatedResult_Test<TValue>(Task<IDomainResult<TValue>> domainValueTask, Func<Task<IDomainResult<TValue>>, TValue> getValueFunc,  Uri urlUri)
	{
		var actionResult = await domainValueTask.ToCustomActionResult(val => new CreatedResult(urlUri, val));

		Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct(actionResult, getValueFunc(domainValueTask));

#if NET6_0_OR_GREATER
		var res = await domainValueTask.ToCustomResult(val => Results.Created(urlUri, val));
		Then_Response_Is_IResult_Type_And_Value_And_Url_Are_Correct(res, getValueFunc(domainValueTask));
#endif
	}
	
	[Theory]
	[MemberData(nameof(DomainResultTaskTestCases))]
	public async Task DomainResult_Task_Converted_To_CreatedResultOfT_Test<TValue>(Task<IDomainResult<TValue>> domainValueTask, Func<Task<IDomainResult<TValue>>, TValue> getValueFunc, Uri urlUri)
	{
		var actionResult = await domainValueTask.ToCustomActionResultOfT(val => new CreatedResult(urlUri, val));

		Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct(actionResult, getValueFunc(domainValueTask));
	}
	public static readonly IEnumerable<object[]> DomainResultTaskTestCases = GetDomainResultTestCases(true);

	[Theory]
	[MemberData(nameof(ValueResultTestCases))]
	public void ValueResult_Converted_To_CreatedResult_Test<TValue>((TValue, IDomainResult) domainValue, Uri urlUri)
	{
		var actionResult = domainValue.ToCustomActionResult(val => new CreatedResult(urlUri, val));

		Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct(actionResult, domainValue.Item1);

#if NET6_0_OR_GREATER
		var res = domainValue.ToCustomResult(val => Results.Created(urlUri, val));
		Then_Response_Is_IResult_Type_And_Value_And_Url_Are_Correct(res, domainValue.Item1);
#endif
	}
	
	[Theory]
	[MemberData(nameof(ValueResultTestCases))]
	public void ValueResult_Converted_To_CreatedResultOfT_Test<TValue>((TValue, IDomainResult) domainValue, Uri urlUri)
	{
		var actionResult = domainValue.ToCustomActionResultOfT(val => new CreatedResult(urlUri, val));

		Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct(actionResult, domainValue.Item1);
	}
	public static readonly IEnumerable<object[]> ValueResultTestCases = GetValueResultTestCases(false);

	[Theory]
	[MemberData(nameof(ValueResultTaskTestCases))]
	public async Task ValueResult_Task_Converted_To_CreatedResult_Test<TValue>(Task<(TValue, IDomainResult)> domainValueTask, Uri urlUri)
	{
		var actionResult = await domainValueTask.ToCustomActionResult(val => new CreatedResult(urlUri, val));

		Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct(actionResult, domainValueTask.Result.Item1);

#if NET6_0_OR_GREATER
		var res = await domainValueTask.ToCustomResult(val => Results.Created(urlUri, val));
		Then_Response_Is_IResult_Type_And_Value_And_Url_Are_Correct(res, domainValueTask.Result.Item1);
#endif		
	}
	
	[Theory]
	[MemberData(nameof(ValueResultTaskTestCases))]
	public async Task ValueResult_Task_Converted_To_CreatedResultOfT_Test<TValue>(Task<(TValue, IDomainResult)> domainValueTask, Uri urlUri)
	{
		var actionResult = await domainValueTask.ToCustomActionResultOfT(val => new CreatedResult(urlUri, val));

		Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct(actionResult, domainValueTask.Result.Item1);
	}
	public static readonly IEnumerable<object[]> ValueResultTaskTestCases = GetValueResultTestCases(true);

	#region Auxiliary methods [PRIVATE] -----------------------------------

	private const string ExpectedUrl = "http://localhost/";

	/// <summary>
	///		The 'THEN' section of the tests, checking the Response type (<see cref="ActionResult"/>), HTTP code, location URL and the returned value
	/// </summary>
	/// <param name="actionResult"> The <see cref="ActionResult"/> in question </param>
	/// <param name="expectedValue"> The expected identification value in the response </param>
	private void Then_Response_Is_ActionResult_Type_And_Value_And_Url_Are_Correct<TValue, TAction>(TAction actionResult, TValue expectedValue)
	{
		// THEN the response type is correct
		CreatedResult? createdResult;
		if (typeof(TAction).IsAssignableFrom(typeof(IActionResult)))
			createdResult = actionResult as CreatedResult;
		else
			createdResult = (actionResult as ActionResult<TValue>)?.Result as CreatedResult;
		Assert.NotNull(createdResult);

		// and the HTTP code is 201
		Assert.Equal(201, createdResult?.StatusCode);
		// and value remains there
		Assert.Equal(expectedValue, createdResult?.Value);
		// and the location URL is correct
		Assert.Equal(ExpectedUrl, createdResult?.Location);
	}

#if NET6_0_OR_GREATER
	/// <summary>
	///		The 'THEN' section of the tests, checking the Response type (<see cref="IResult"/>), HTTP code, location URL and the returned value
	/// </summary>
	private void Then_Response_Is_IResult_Type_And_Value_And_Url_Are_Correct<TValue>(IResult res, TValue expectedValue)
	{
		// THEN the response type is correct
		res.AssertCreatedResultType();

		// and the HTTP code is 201
		Assert.Equal(201, res.GetPropValue("StatusCode"));
		// and the value remains there
		Assert.Equal(expectedValue, res.GetPropValue());
		// and the location URL is correct
		Assert.Equal(ExpectedUrl, res.GetPropValue("Location"));
	}
#endif

	private static IEnumerable<object[]> GetDomainResultTestCases(bool wrapInTask)
		=> new List<object[]>
			{
				GetDomainResultTestCase(10,  wrapInTask),						// E.g. { DomainResult.Success(10), res => res.Value }
				GetDomainResultTestCase("1",  wrapInTask),
				GetDomainResultTestCase(new TestDto("1"),  wrapInTask)
			};

	private static IEnumerable<object[]> GetValueResultTestCases(bool wrapInTask)
		=> new List<object[]>
			{
				GetValueResultTestCase(10,  wrapInTask),						// E.g. { DomainResult.Success(10), res => res.Value }
				GetValueResultTestCase("1",  wrapInTask),
				GetValueResultTestCase(new TestDto("1"), wrapInTask)
			};

	private static object[] GetDomainResultTestCase<T>(T domainValue, bool wrapInTask = false)
		=> new [] {
			wrapInTask
				? DomainResult.SuccessTask(domainValue) as object
				: DomainResult.Success(domainValue),
			wrapInTask
				? (Func<Task<IDomainResult<T>>, T>)(res => res.Result.Value) as object
				: (Func<IDomainResult<T>, T>)(res => res.Value),
			new Uri(ExpectedUrl)
		};

	private static object[] GetValueResultTestCase<T>(T domainValue, bool wrapInTask = false)
		=> new [] 
		{
			wrapInTask  ? Task.FromResult((domainValue, IDomainResult.Success())) as object
						: (domainValue, IDomainResult.Success()),
			new Uri(ExpectedUrl)
		};
	
	#endregion // Auxiliary methods [PRIVATE] -----------------------------
}