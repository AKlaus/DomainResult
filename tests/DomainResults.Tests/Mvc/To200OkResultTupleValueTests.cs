using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class To_200_OkResult_TupleValue_Tests
	{
		#region Test of successful '(TValue, IDomainResult)' response conversion ------------------

		[Theory]
		[MemberData(nameof(SuccessfulTestCases))]
		public void ValueResult_Converted_ToActionResult_Test<TValue>((TValue, IDomainResult) tupleValue)
		{
			// WHEN convert a value to ActionResult
			var actionRes = tupleValue.ToActionResult();
			// and to ActionResult<T>
			var actionResOfT = tupleValue.ToActionResultOfT();

			// THEN the response type is correct
			var okResult = actionRes as OkObjectResult;
			Assert.NotNull(okResult);
			Assert.NotNull(actionResOfT);

			// and value remains there
			Assert.Equal(tupleValue.Item1!, okResult!.Value);
			Assert.Equal(tupleValue.Item1, actionResOfT.Value);
		}

		public static readonly IEnumerable<object[]> SuccessfulTestCases = new List<object[]>
		{
			new object[] { (10,  IDomainResult.Success()) },
			new object[] { ("1", IDomainResult.Success()) },
			new object[] { (new TestDto("1"), IDomainResult.Success()) }
		};
		#endregion // Test of successful '(TValue, IDomainResult)' response conversion ------------

		#region Test of successful 'Task<(TValue, IDomainResult)>' response conversion ------------

		[Theory]
		[MemberData(nameof(SuccessfulTaskTestCases))]
		public async Task ValueResult_Task_Converted_ToActionResult_Test<TValue>(Task<(TValue, IDomainResult)> tupleValueTask)
		{
			// WHEN convert a value to ActionResult
			var actionRes = await tupleValueTask.ToActionResult();
			// and to ActionResult<T>
			var actionResOfT = await tupleValueTask.ToActionResultOfT();

			// THEN the response type is correct
			var okResult = actionRes as OkObjectResult;
			Assert.NotNull(okResult);
			Assert.NotNull(actionResOfT);

			// and value remains there
			Assert.Equal((await tupleValueTask).Item1!, okResult!.Value);
			Assert.Equal((await tupleValueTask).Item1, actionResOfT.Value);
		}

		public static readonly IEnumerable<object[]> SuccessfulTaskTestCases = new List<object[]>
		{
			new object[] { Task.FromResult((10,  IDomainResult.Success())) },
			new object[] { Task.FromResult(("1", IDomainResult.Success())) },
			new object[] { Task.FromResult((new TestDto("1"), IDomainResult.Success())) }
		};
		#endregion // Test of successful 'Task<(TValue, IDomainResult)>' response conversion ------
	}
}
