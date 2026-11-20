using System.Diagnostics.CodeAnalysis;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class To_204_NoContentResult_Success_Tests
{
	[Fact]
	public void DomainResultConverted_To_NoContent()
	{
		// GIVEN a successful domain result
		var domainRes = DomainResult.Success();

		// WHEN convert it to ActionResult
		var actionRes = domainRes.ToActionResult();

		// THEN the response type is correct
		Assert.IsType<NoContentResult>(actionRes);
	}
	
	[Fact]
	public void DomainResultConverted_To_NoContent_of_IResult()
	{
		// GIVEN a successful domain result
		var domainRes = DomainResult.Success();

		// WHEN convert it to IResult
		var res = domainRes.ToResult();

		// THEN the response type is NoContent
		res.AssertNoContentResultType();
	}
	
	[Fact]
	public async Task DomainResult_Task_Converted_To_NoContent()
	{
		// GIVEN a successful domain result
		var domainRes = DomainResult.SuccessTask();

		// WHEN convert it to ActionResult
		var actionRes = await domainRes.ToActionResult();

		// THEN the response type is correct
		Assert.IsType<NoContentResult>(actionRes);
	}
	
	[Fact]
	public async Task DomainResult_Task_Converted_To_NoContent_of_IResult()
	{
		// GIVEN a successful domain result
		var domainRes = DomainResult.SuccessTask();

		// WHEN convert it to IResult
		var res = await domainRes.ToResult();

		// THEN the response type is NoContent
		res.AssertNoContentResultType();
	}

	[Theory]
	[MemberData(nameof(NullValueTestCases))]
	public void DomainResult_With_Null_Value_Converted_To_NoContent<TValue>(IDomainResult<TValue> domainRes, (TValue, IDomainResult) tupleRes)
	{
		// GIVEN a successful domain result with a null value, in each result shape

		// WHEN convert it to ActionResult
		IActionResult[] actionRes = [domainRes.ToActionResult(), tupleRes.ToActionResult()];
		// and to ActionResult<T>
		ActionResult<TValue>[] actionResOfT = [domainRes.ToActionResultOfT(), tupleRes.ToActionResultOfT()];

		// THEN the response type is NoContent
		Assert.All(actionRes, res => Assert.IsType<NoContentResult>(res));
		Assert.All(actionResOfT, res => Assert.IsType<NoContentResult>(res.Result));
	}

	[Theory]
	[MemberData(nameof(NullValueTestCases))]
	public void DomainResult_With_Null_Value_Converted_To_NoContent_of_IResult<TValue>(IDomainResult<TValue> domainRes, (TValue, IDomainResult) tupleRes)
	{
		// GIVEN a successful domain result with a null value, in each result shape

		// WHEN convert it to IResult
		IResult[] results = [domainRes.ToResult(), tupleRes.ToResult()];

		// THEN the response type is NoContent
		Assert.All(results, res => res.AssertNoContentResultType());
	}

	/// <summary>
	///		Null values of a reference type and a nullable value type
	/// </summary>
	public static readonly IEnumerable<object[]> NullValueTestCases =
	[
		GetTestCase<TestDto?>(null),
		GetTestCase<int?>(null)
	];

	private static object[] GetTestCase<T>(T value)
		=> [DomainResult.Success(value), IDomainResult.Success(value)];
}