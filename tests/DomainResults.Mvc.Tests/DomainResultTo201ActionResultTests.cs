using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AK.DomainResults.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class DomainResult_To_201_ActionResult_Tests
	{
		[Theory]
		[MemberData(nameof(SuccessfulTestCases))]
		public void Successful_DomainResult_To_CreatedResult_Test<TValue>(IDomainResult<TValue> domainValue, Func<IDomainResult<TValue>, TValue> getValueFunc)
		{
			// WHEN convert a value to ActionResult
			var actionRes = domainValue.ToCreatedResult("/test");

			// THEN the response type is correct
			var createdResult = actionRes as CreatedResult;
			Assert.NotNull(createdResult);

			// and value remains there
			Assert.Equal(getValueFunc(domainValue), createdResult.Value);
		}
		public static readonly IEnumerable<object[]> SuccessfulTestCases = GetTestCases(false);

		[Theory]
		[MemberData(nameof(SuccessfulTaskTestCases))]
		public async Task Successful_DomainResult_Task_To_CreatedResult_Test<TValue>(Task<IDomainResult<TValue>> domainValueTask, Func<Task<IDomainResult<TValue>>, TValue> getValueFunc)
		{
			// WHEN convert a value to ActionResult
			var actionRes = await domainValueTask.ToCreatedResult("/test");

			// THEN the response type is correct
			var createdResult = actionRes as CreatedResult;
			Assert.NotNull(createdResult);

			// and value remains there
			Assert.Equal(getValueFunc(domainValueTask), createdResult.Value);
		}
		public static readonly IEnumerable<object[]> SuccessfulTaskTestCases = GetTestCases(true);

		private static IEnumerable<object[]> GetTestCases(bool wrapInTask)
			=> new List<object[]>
				{
					GetTestCase(10,  wrapInTask),							// E.g. { DomainResult.Success(10), res => res.Value }
					GetTestCase("1", wrapInTask),
					GetTestCase(new TestDto { Prop = "1" }, wrapInTask)
				};

		private static object[] GetTestCase<T>(T domainValue, bool wrapInTask = false)
			=> wrapInTask
				? new object[] {
					DomainResult.SuccessTask(domainValue),
					(Func<Task<IDomainResult<T>>, T>)(res => res.Result.Value)
				 }
				: new object[] {
					DomainResult.Success(domainValue),
					(Func<IDomainResult<T>, T>)(res => res.Value)
				 };
	}
}
