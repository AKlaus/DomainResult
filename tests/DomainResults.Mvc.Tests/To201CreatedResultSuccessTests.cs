using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AK.DomainResults.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class To_201_CreatedResult_Success_Tests
	{
		[Theory]
		[MemberData(nameof(DomainResultTestCases))]
		public void DomainResult_Converted_To_CreatedResult_Test<TValue>(IDomainResult<TValue> domainValue, Func<IDomainResult<TValue>, TValue> getValueFunc, string urlString, Uri urlUri)
		{
			var actionResult = urlUri == null ? domainValue.ToCreatedResult(urlString)
											  : domainValue.ToCreatedResult(urlUri);

			Then_ResponseType_And_Value_And_Url_Are_Correct(actionResult, getValueFunc(domainValue));
		}
		public static readonly IEnumerable<object[]> DomainResultTestCases = GetDomainResultTestCases(false);

		[Theory]
		[MemberData(nameof(DomainResultTaskTestCases))]
		public async Task DomainResult_Task_Converted_To_CreatedResult_Test<TValue>(Task<IDomainResult<TValue>> domainValueTask, Func<Task<IDomainResult<TValue>>, TValue> getValueFunc, string urlString, Uri urlUri)
		{
			var actionResult = await (urlUri == null ? domainValueTask.ToCreatedResult(urlString)
													 : domainValueTask.ToCreatedResult(urlUri));

			Then_ResponseType_And_Value_And_Url_Are_Correct(actionResult, getValueFunc(domainValueTask));
		}
		public static readonly IEnumerable<object[]> DomainResultTaskTestCases = GetDomainResultTestCases(true);

		[Theory]
		[MemberData(nameof(ValueResultTestCases))]
		public void ValueResult_Converted_To_CreatedResult_Test<TValue>((TValue, IDomainResult) domainValue, string urlString, Uri urlUri)
		{
			var actionResult = urlUri == null ? domainValue.ToCreatedResult(urlString)
											  : domainValue.ToCreatedResult(urlUri);

			Then_ResponseType_And_Value_And_Url_Are_Correct(actionResult, domainValue.Item1);
		}
		public static readonly IEnumerable<object[]> ValueResultTestCases = GetValueResultTestCases(false);

		[Theory]
		[MemberData(nameof(ValueResultTaskTestCases))]
		public async Task ValueResult_Task_Converted_To_CreatedResult_Test<TValue>(Task<(TValue, IDomainResult)> domainValueTask, string urlString, Uri urlUri)
		{
			var actionResult = await (urlUri == null ? domainValueTask.ToCreatedResult(urlString)
													 : domainValueTask.ToCreatedResult(urlUri));

			Then_ResponseType_And_Value_And_Url_Are_Correct(actionResult, domainValueTask.Result.Item1);
		}
		public static readonly IEnumerable<object[]> ValueResultTaskTestCases = GetValueResultTestCases(true);

		#region Auxiliary methods [PRIVATE] -----------------------------------

		private const string expectedUrl = "http://localhost/";

		/// <summary>
		///		The 'THEN' section of the tests, checking the Response type, HTTP code, location URL and the returned value
		/// </summary>
		/// <param name="actionResult"> The <see cref="IActionResult"/> in question </param>
		/// <param name="expectedValue"> The expected identification value in the response </param>
		private void Then_ResponseType_And_Value_And_Url_Are_Correct<TValue>(IActionResult actionResult, TValue expectedValue)
		{
			// THEN the response type is correct
			var createdResult = actionResult as CreatedResult;
			Assert.NotNull(createdResult);
			Assert.Equal(201, createdResult.StatusCode);

			// and value remains there
			Assert.Equal(expectedValue, createdResult.Value);

			// and the location URL is correct
			Assert.Equal(expectedUrl, createdResult.Location);
		}

		private static IEnumerable<object[]> GetDomainResultTestCases(bool wrapInTask)
			=> new List<object[]>
				{
					GetDomainResultTestCase(10,  false, wrapInTask),						// E.g. { DomainResult.Success(10), res => res.Value }
					GetDomainResultTestCase("1", true,  wrapInTask),
					GetDomainResultTestCase(new TestDto { Prop = "1" }, true,  wrapInTask)
				};

		private static IEnumerable<object[]> GetValueResultTestCases(bool wrapInTask)
			=> new List<object[]>
				{
					GetValueResultTestCase(10,  false, wrapInTask),						// E.g. { DomainResult.Success(10), res => res.Value }
					GetValueResultTestCase("1", true,  wrapInTask),
					GetValueResultTestCase(new TestDto { Prop = "1" }, true, wrapInTask)
				};

		private static object[] GetDomainResultTestCase<T>(T domainValue, bool urlAsString, bool wrapInTask = false)
			=> new object[] {
				wrapInTask
					? DomainResult.SuccessTask(domainValue) as object
					: DomainResult.Success(domainValue),
				wrapInTask
					? (Func<Task<IDomainResult<T>>, T>)(res => res.Result.Value) as object
					: (Func<IDomainResult<T>, T>)(res => res.Value),
				urlAsString ? expectedUrl : null,
				urlAsString ? null: new Uri(expectedUrl)
			};

		private static object[] GetValueResultTestCase<T>(T domainValue, bool urlAsString, bool wrapInTask = false)
			=> new object[] 
			{
				wrapInTask  ? Task.FromResult((domainValue, IDomainResult.Success())) as object
							: (domainValue, IDomainResult.Success()),
				urlAsString ? expectedUrl : null,
				urlAsString ? null: new Uri(expectedUrl)
			};
		#endregion // Auxiliary methods [PRIVATE] -----------------------------
	}
}
