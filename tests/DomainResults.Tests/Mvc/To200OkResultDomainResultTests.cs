using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class To_200_OkResult_DomainResult_Tests
{
	#region Test of successful 'IDomainResult<TValue>' response conversion -----

	[Theory]
	[MemberData(nameof(SuccessfulTestCases))]
	public void DomainResult_With_Value_Converted_ToActionResult_Test<TValue>(IDomainResult<TValue> domainValue)
	{
		// WHEN convert a value to ActionResult
		var actionRes = domainValue.ToActionResult();
		// and to ActionResult<T>
		var actionResOfT = domainValue.ToActionResultOfT();

		// THEN the response type is correct
		var okResult = actionRes as OkObjectResult;
		Assert.NotNull(okResult);
		Assert.NotNull(actionResOfT);

		// and value remains there
		Assert.Equal(domainValue.Value!, okResult!.Value);
		Assert.Equal(domainValue.Value, actionResOfT.Value);
	}
	
	[Theory]
	[MemberData(nameof(SuccessfulTestCases))]
	public void DomainResult_With_Value_Converted_ToIResult_Test<TValue>(IDomainResult<TValue> domainValue)
	{
		// WHEN convert a value to IResult
		var res = domainValue.ToResult();

		// THEN the response type OK with correct value 
		res.AssertOkObjectResultTypeAndValue(domainValue.Value!);
	}
	public static readonly IEnumerable<object[]> SuccessfulTestCases = GetTestCases(false);
	
	#endregion // Test of successful 'IDomainResult<TValue>' response conversion

	#region Test of successful 'Task<IDomainResult<TValue>>' response conversion
	
	[Theory]
	[MemberData(nameof(SuccessfulTaskTestCases))]
	public async Task DomainResult_With_Value_Task_Converted_ToActionResult_Test<TValue>(Task<IDomainResult<TValue>> domainValueTask)
	{
		// WHEN convert a value to ActionResult
		var actionRes = await domainValueTask.ToActionResult();
		// and to ActionResult<T>
		var actionResOfT = await domainValueTask.ToActionResultOfT();

		// THEN the response type is correct
		var okResult = actionRes as OkObjectResult;
		Assert.NotNull(okResult);
		Assert.NotNull(actionResOfT);

		// and value remains there
		var domainValue = await domainValueTask;
		Assert.Equal(domainValue.Value!, okResult!.Value);
		Assert.Equal(domainValue.Value, actionResOfT.Value);
	}
	
	[Theory]
	[MemberData(nameof(SuccessfulTaskTestCases))]
	public async Task DomainResult_With_Value_Task_Converted_ToIResult_Test<TValue>(Task<IDomainResult<TValue>> domainValueTask)
	{
		// WHEN convert a value to IResult
		var res = await domainValueTask.ToResult();

		// THEN the response type OK with the correct value 
		res.AssertOkObjectResultTypeAndValue((await domainValueTask).Value!);
	}
	public static readonly IEnumerable<object[]> SuccessfulTaskTestCases = GetTestCases(true);

	#endregion // Test of successful 'Task<IDomainResult<TValue>>' response conversion

	private static IEnumerable<object[]> GetTestCases(bool wrapInTask)
		=> new List<object[]> 
			{ 
				GetTestCase(10,  true, wrapInTask),			// E.g. { DomainResult.Success(10), res => res.Value }
				GetTestCase(10,  false, wrapInTask),
				GetTestCase("1", true, wrapInTask),
				GetTestCase("1", false, wrapInTask),
				GetTestCase(new TestDto("1"), true, wrapInTask) 
			};

	private static object[] GetTestCase<T>(T domainValue, bool genericClass, bool wrapInTask = false)
		=> wrapInTask 
			? [ genericClass ? DomainResult<T>.SuccessTask(domainValue)	: DomainResult.SuccessTask(domainValue) ]
			: [ genericClass ? DomainResult<T>.Success(domainValue)		: DomainResult.Success(domainValue) ];
}