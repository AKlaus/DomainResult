using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AK.DomainResults.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class DomainResultValueToOkResultTests
	{
		#region Test of successful 'IDomainResult<TValue>' response conversion --------------------

		[Theory]
		[MemberData(nameof(SuccessfulTestCases))]
		public void SuccessfulValueResult<TValue>(IDomainResult<TValue> domainValue, Func<OkObjectResult, TValue> getValueFunc)
		{
			// WHEN convert a value to ActionResult
			var actionRes = domainValue.ToActionResult();

			// THEN the response type is correct
			var okResult = actionRes as OkObjectResult;
			Assert.NotNull(okResult);

			// and value remains there
			Assert.Equal(domainValue.Value, getValueFunc(okResult));
		}

		public static readonly IEnumerable<object[]> SuccessfulTestCases = new List<object[]>
		{
			new object[] {	DomainResult.Success(10), 
							(Func<OkObjectResult, int>)(res => (int)res.Value) 
						 },
			new object[] {	DomainResult.Success("1"), 
							(Func<OkObjectResult, string>)(res => (string)res.Value) 
						 },
			new object[] {  DomainResult.Success(new TestDto { Prop = "1" }), 
							(Func<OkObjectResult, TestDto>)(res => (TestDto)res.Value) 
						 },
		};
		#endregion // Test of successful 'IDomainResult<TValue>' response conversion --------------

		#region Test of successful 'Task<IDomainResult<TValue>>' response conversion --------------

		[Theory]
		[MemberData(nameof(SuccessfulTaskTestCases))]
		public async Task SuccessfulValueResultTask<TValue>(Task<IDomainResult<TValue>> domainValueTask, Func<OkObjectResult, TValue> getValueFunc)
		{
			// WHEN convert a value to ActionResult
			var actionRes = await domainValueTask.ToActionResult();

			// THEN the response type is correct
			var okResult = actionRes as OkObjectResult;
			Assert.NotNull(okResult);

			// and value remains there
			Assert.Equal(domainValueTask.Result.Value, getValueFunc(okResult));
		}

		public static readonly IEnumerable<object[]> SuccessfulTaskTestCases = new List<object[]>
		{
			new object[] {  DomainResult.SuccessTask(10),
							(Func<OkObjectResult, int>)(res => (int)res.Value)
						 },
			new object[] {  DomainResult.SuccessTask("1"),
							(Func<OkObjectResult, string>)(res => (string)res.Value)
						 },
			new object[] {  DomainResult.SuccessTask(new TestDto { Prop = "1" }),
							(Func<OkObjectResult, TestDto>)(res => (TestDto)res.Value)
						 },
		};
		#endregion // Test of successful 'Task<IDomainResult<TValue>>' response conversion --------
	}
}
