using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AK.DomainResults.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class ValueResult_To_OkResult_Tests
	{
		#region Test of successful '(TValue, IDomainResult)' response conversion ------------------

		[Theory]
		[MemberData(nameof(SuccessfulTestCases))]
		public void ValueResult_Converted_ToActionResult_Test<TValue>((TValue, IDomainResult) tupleValue, Func<OkObjectResult, TValue> getValueFunc)
		{
			// WHEN convert a value to ActionResult
			var actionRes = tupleValue.ToActionResult();

			// THEN the response type is correct
			var okResult = actionRes as OkObjectResult;
			Assert.NotNull(okResult);

			// and value remains there
			Assert.Equal(tupleValue.Item1, getValueFunc(okResult));
		}

		public static readonly IEnumerable<object[]> SuccessfulTestCases = new List<object[]>
		{
			new object[] {  (10,  ErrorDetails.Success()),
							(Func<OkObjectResult, int>)(res => (int)res.Value)
						 },
			new object[] {  ("1", ErrorDetails.Success()),
							(Func<OkObjectResult, string>)(res => (string)res.Value)
						 },
			new object[] {  (new TestDto { Prop = "1" }, ErrorDetails.Success()),
							(Func<OkObjectResult, TestDto>)(res => (TestDto)res.Value)
						 },
		};
		#endregion // Test of successful '(TValue, IDomainResult)' response conversion ------------

		#region Test of successful 'Task<(TValue, IDomainResult)>' response conversion ------------

		[Theory]
		[MemberData(nameof(SuccessfulTaskTestCases))]
		public async Task ValueResult_Task_Converted_ToActionResult_Test<TValue>(Task<(TValue, IDomainResult)> tupleValueTask, Func<OkObjectResult, TValue> getValueFunc)
		{
			// WHEN convert a value to ActionResult
			var actionRes = await tupleValueTask.ToActionResult();

			// THEN the response type is correct
			var okResult = actionRes as OkObjectResult;
			Assert.NotNull(okResult);

			// and value remains there
			Assert.Equal((await tupleValueTask).Item1, getValueFunc(okResult));
		}

		public static readonly IEnumerable<object[]> SuccessfulTaskTestCases = new List<object[]>
		{
			new object[] {  Task.FromResult((10,  ErrorDetails.Success())),
							(Func<OkObjectResult, int>)(res => (int)res.Value)
						 },
			new object[] {  Task.FromResult(("1", ErrorDetails.Success())),
							(Func<OkObjectResult, string>)(res => (string)res.Value)
						 },
			new object[] {  Task.FromResult((new TestDto { Prop = "1" }, ErrorDetails.Success())),
							(Func<OkObjectResult, TestDto>)(res => (TestDto)res.Value)
						 },
		};
		#endregion // Test of successful 'Task<(TValue, IDomainResult)>' response conversion ------
	}
}
