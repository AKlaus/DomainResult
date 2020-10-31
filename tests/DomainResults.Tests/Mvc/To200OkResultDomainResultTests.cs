using System.Collections.Generic;
using System.Threading.Tasks;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc
{
	public class To_200_OkResult_DomainResult_Tests
	{
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
			Assert.Equal(domainValue.Value, okResult.Value);
			Assert.Equal(domainValue.Value, actionResOfT.Value);
		}
		public static readonly IEnumerable<object[]> SuccessfulTestCases = GetTestCases(false);

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
			Assert.Equal(domainValueTask.Result.Value, okResult.Value);
			Assert.Equal(domainValueTask.Result.Value, actionResOfT.Value);
		}
		public static readonly IEnumerable<object[]> SuccessfulTaskTestCases = GetTestCases(true);

		private static IEnumerable<object[]> GetTestCases(bool wrapInTask)
			=> new List<object[]> 
				{ 
					GetTestCase(10,  true, wrapInTask),							// E.g. { DomainResult.Success(10), res => res.Value }
					GetTestCase(10,  false, wrapInTask),
					GetTestCase("1", true, wrapInTask),
					GetTestCase("1", false, wrapInTask),
					GetTestCase(new TestDto { Prop = "1" }, true, wrapInTask) 
				};

		private static object[] GetTestCase<T>(T domainValue, bool genericClass, bool wrapInTask = false)
			=> wrapInTask 
				? new object[] {
					genericClass ? DomainResult<T>.SuccessTask(domainValue) : DomainResult.SuccessTask(domainValue)
				 }
				: new object[] {
					genericClass ? DomainResult<T>.Success(domainValue) : DomainResult.Success(domainValue)
				 };
	}
}