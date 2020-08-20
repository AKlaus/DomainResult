using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AK.DomainResults.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class ValueResultToActionTests
	{
		#region Test of successful '(TValue, IDomainResult)' response conversion ------------------

		[Theory]
		[MemberData(nameof(SuccessfulTestCases))]
		public void SuccessfulNumericValueResult<TValue>((TValue, IDomainResult) tupleValue, Func<OkObjectResult, TValue> getValueFunc)
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
			new object[] {	(10,  ErrorDetails.Success()), 
							(Func<OkObjectResult, int>)(res => (int)res.Value) 
						 },
			new object[] {	("1", ErrorDetails.Success()), 
							(Func<OkObjectResult, string>)(res => (string)res.Value) 
						 },
			new object[] {	(new TestDto { Prop = "1" }, ErrorDetails.Success()), 
							(Func<OkObjectResult, TestDto>)(res => (TestDto)res.Value) 
						 },
		};
		#endregion // Test of successful '(TValue, IDomainResult)' response conversion ------------

		#region Test of successful 'Task<(TValue, IDomainResult)>' response conversion ------------

		[Theory]
		[MemberData(nameof(SuccessfulTaskTestCases))]
		public async Task SuccessfulNumericValueResultTask<TValue>(Task<(TValue, IDomainResult)> tupleValueTask, Func<OkObjectResult, TValue> getValueFunc)
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

		#region Test of failed '(TValue, IDomainResult)' response conversion ----------------------

		[Theory]
		[MemberData(nameof(FailedTestCases))]
		public void FailedNumericValueResult<TValue>((TValue, IDomainResult) tupleValue, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			// WHEN convert a value to ActionResult
			var actionRes = tupleValue.ToActionResult();

			// THEN the response type is correct
			var objResult = actionRes as ObjectResult;
			Assert.NotNull(objResult);
			var problemDetails = objResult.Value as ProblemDetails;
			Assert.NotNull(problemDetails);

			// and the ProblemDetails properties are as expected
			Assert.Equal(expectedCode,		problemDetails.Status);
			Assert.Equal(expectedTitle,		problemDetails.Title);
			Assert.Equal(expectedErrorMsg,	problemDetails.Detail);
		}

		public static readonly IEnumerable<object[]> FailedTestCases = new List<object[]>
		{
			new object[] { (10, ErrorDetails.Error(new [] { "1" })),		400, "Bad Request", "1" },
			new object[] { (10, ErrorDetails.Error(new [] { "1", "2" })),	400, "Bad Request", "1, 2" },
			new object[] { (10, ErrorDetails.NotFound(new [] { "1" })),		404, "Not Found",	"1" },
			new object[] { (10, ErrorDetails.NotFound(new [] { "1", "2" })),404, "Not Found",	"1, 2" },
		};
		#endregion // Test of failed '(TValue, IDomainResult)' response conversion ----------------

		#region Test of failed 'Task<(TValue, IDomainResult)>' response conversion ----------------

		[Theory]
		[MemberData(nameof(FailedTaskTestCases))]
		public async Task FailedNumericValueResultTask<TValue>(Task<(TValue, IDomainResult)> tupleValueTask, int expectedCode, string expectedTitle, string expectedErrorMsg)
		{
			// WHEN convert a value to ActionResult
			var actionRes = await tupleValueTask.ToActionResult();

			// THEN the response type is correct
			var objResult = actionRes as ObjectResult;
			Assert.NotNull(objResult);
			var problemDetails = objResult.Value as ProblemDetails;
			Assert.NotNull(problemDetails);

			// and the ProblemDetails properties are as expected
			Assert.Equal(expectedCode, problemDetails.Status);
			Assert.Equal(expectedTitle, problemDetails.Title);
			Assert.Equal(expectedErrorMsg, problemDetails.Detail);
		}

		public static readonly IEnumerable<object[]> FailedTaskTestCases = new List<object[]>
		{
			new object[] { Task.FromResult((10, ErrorDetails.Error(new [] { "1" }))),        400, "Bad Request", "1" },
			new object[] { Task.FromResult((10, ErrorDetails.Error(new [] { "1", "2" }))),   400, "Bad Request", "1, 2" },
			new object[] { Task.FromResult((10, ErrorDetails.NotFound(new [] { "1" }))),     404, "Not Found",   "1" },
			new object[] { Task.FromResult((10, ErrorDetails.NotFound(new [] { "1", "2" }))),404, "Not Found",   "1, 2" },
		};
		#endregion // Test of failed 'Task<(TValue, IDomainResult)>' response conversion ----------

		class TestDto
		{
			public string Prop { get; set; }
		}
	}
}
