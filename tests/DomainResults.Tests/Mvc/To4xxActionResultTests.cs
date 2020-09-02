using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class To_4xx_ActionResult_Tests
	{
		[Theory]
		[MemberData(nameof(FailedResultWithValueCases))]
		public void Failed_DomainResult_With_Value<TValue>(IDomainResult<TValue> domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			var actionRes = domainValue.ToActionResult();

			Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
		}		
		public static readonly IEnumerable<object[]> FailedResultWithValueCases = TestCases(DomainResult.Error<int>, DomainResult.NotFound<int>);

		[Theory]
		[MemberData(nameof(FailedResultWithValueTaskCases))]
		public async Task Failed_DomainResult_With_Value_Task<TValue>(Task<IDomainResult<TValue>> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			var actionRes = await domainValueTask.ToActionResult();

			Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
		}
		public static readonly IEnumerable<object[]> FailedResultWithValueTaskCases = TestCases(DomainResult.ErrorTask<int>, DomainResult.NotFoundTask<int>);

		[Theory]
		[MemberData(nameof(FailedResultCases))]
		public void Failed_DomainResult(IDomainResult domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			var actionRes = domainValue.ToActionResult();

			Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
		}
		public static readonly IEnumerable<object[]> FailedResultCases = TestCases(DomainResult.Error, DomainResult.NotFound);

		[Theory]
		[MemberData(nameof(FailedResultTaskCases))]
		public async Task Failed_DomainResult_Task(Task<IDomainResult> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			var actionRes = await domainValueTask.ToActionResult();

			Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
		}
		public static readonly IEnumerable<object[]> FailedResultTaskCases = TestCases(DomainResult.ErrorTask, DomainResult.NotFoundTask);

		[Theory]
		[MemberData(nameof(FailedValueTestCases))]
		public void Failed_ValueResult((int, IDomainResult) domainValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			var actionRes = domainValue.ToActionResult();

			Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
		}
		public static readonly IEnumerable<object[]> FailedValueTestCases = TestCases(DomainResult.Error, DomainResult.NotFound, v => (10, v));

		[Theory]
		[MemberData(nameof(FailedValueTaskTestCases))]
		public async Task Failed_ValueResult_Task(Task<(int, IDomainResult)> domainValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			var actionRes = await domainValueTask.ToActionResult();

			Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(actionRes, expectedCode, expectedTitle, expectedErrorMsg);
		}
		public static readonly IEnumerable<object[]> FailedValueTaskTestCases = TestCases(DomainResult.Error, DomainResult.NotFound, v => Task.FromResult((10, v)));

		#region Auxiliary methods [PRIVATE] -----------------------------------

		/// <summary>
		///		The 'THEN' section of the tests, checking the Response type and the <see cref="ProblemDetails"/>
		/// </summary>
		/// <param name="actionResult"> The <see cref="IActionResult"/> in question </param>
		/// <param name="expectedCode"> The expected HTTP code </param>
		/// <param name="expectedTitle"> The expected title in the <see cref="ProblemDetails"/> </param>
		/// <param name="expectedErrorMsg"> The expected description messsages in the <see cref="ProblemDetails"/> </param>
		private void Then_ResponseType_Correct_And_ProblemDetails_StatusAndText_Correct(IActionResult actionResult, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			// THEN the response type is correct
			var objResult = actionResult as ObjectResult;
			Assert.NotNull(objResult);
			var problemDetails = objResult.Value as ProblemDetails;
			Assert.NotNull(problemDetails);

			// and the ProblemDetails properties are as expected
			Assert.Equal(expectedCode, problemDetails.Status);
			Assert.Equal(expectedTitle, problemDetails.Title);
			Assert.Equal(expectedErrorMsg, problemDetails.Detail);
		}

		/// <summary>
		///		Get test cases (input test parameters for the <see cref="MemberDataAttribute"/>)
		/// </summary>
		/// <param name="domainErrorFunc"> The 'Error' function </param>
		/// <param name="domainNotFoundFunc"> The 'NotFound' function </param>
		/// <param name="wrapInFunc"> Optional wrapper <see cref="TupleValue"/> of the Domain result method </param>
		/// <returns> Input test parameters </returns>
		private static IEnumerable<object[]> TestCases<T>(Func<IEnumerable<string>,T> domainErrorFunc, Func<IEnumerable<string>, T> domainNotFoundFunc, Func<T, object> wrapInFunc = null)
		{
			Func<T, object> optionalWrapper 
				= (T value) => wrapInFunc != null ? wrapInFunc(value) : value as object;

			var returnValues = new List<object[]>
				{
					new object[] { optionalWrapper(domainErrorFunc(new[] { "1" })),			DomainResultExtensions.Conventions.ErrorHttpCode,	 "Bad Request", "1" },
					new object[] { optionalWrapper(domainErrorFunc(new[] { "1", "2" })),	DomainResultExtensions.Conventions.ErrorHttpCode,	 "Bad Request", "1, 2" },
					new object[] { optionalWrapper(domainNotFoundFunc(new[] { "1" })),		DomainResultExtensions.Conventions.NotFoundHttpCode, "Not Found",   "1" },
					new object[] { optionalWrapper(domainNotFoundFunc(new[] { "1", "2" })), DomainResultExtensions.Conventions.NotFoundHttpCode, "Not Found",   "1, 2" },
				};
			foreach (var val in returnValues)
				yield return val;
		}
		#endregion // Auxiliary methods [PRIVATE] -----------------------------
	}
}
