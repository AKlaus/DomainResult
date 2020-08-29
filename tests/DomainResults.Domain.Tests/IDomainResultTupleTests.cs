using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using DomainResults.Domain;

using Xunit;

namespace DomainResults.Domain.Tests
{
	public class IDomainResult_Tuple_Tests
	{
		#region Test of '(TValue, IDomainResult)' responses -------------------

		[Theory]
		[MemberData(nameof(TestCases))]
		public void ValueResultResponseTest(Func<(int, IDomainResult)> method, DomainOperationStatus expectedStatus, IEnumerable<string> expectedErrMessages)
		{
			var (value, domainResult) = method();

			if (expectedStatus == DomainOperationStatus.Success)
			{
				Assert.True(domainResult.IsSuccess);
				Assert.True(value > 0);
			}

			Assert.Equal(expectedStatus, domainResult.Status);
			Assert.Equal(expectedErrMessages, domainResult.Errors);
		}

		public static IEnumerable<object[]> TestCases
		{
			get
			{	foreach (var t in testCases)
					yield return t;
			}
		}

		private static readonly IEnumerable<object[]> testCases = new List<object[]>
		{
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Success(10)), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.NotFound<int>("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.NotFound<int>(new[] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Error<int>("1")), DomainOperationStatus.Error, new[] { "1" } },
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Error<int>(new[] { "1", "2" })), DomainOperationStatus.Error, new[] { "1", "2" } },
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Error<int>(new[] { new ValidationResult("1") })), DomainOperationStatus.Error, new[] { "1" } }
		};
		#endregion // Test of '(TValue, IDomainResult)' responses -------------

		#region Test of 'Task<(TValue, IDomainResult)>' responses -------------

		[Theory]
		[MemberData(nameof(TestCasesWrappedInTask))]
		public async Task TaskValueResultResponseTest(Func<Task<(int, IDomainResult)>> method, DomainOperationStatus expectedStatus, IEnumerable<string> expectedErrMessages)
		{
			var (value, domainResult) = await method();

			if (expectedStatus == DomainOperationStatus.Success)
			{
				Assert.True(domainResult.IsSuccess);
				Assert.True(value > 0);
			}

			Assert.Equal(expectedStatus, domainResult.Status);
			Assert.Equal(expectedErrMessages, domainResult.Errors);
		}

		public static IEnumerable<object[]> TestCasesWrappedInTask
		{
			get
			{
				foreach (var t in testCasesWrappedInTask)
					yield return t;
			}
		}

		private static readonly IEnumerable<object[]> testCasesWrappedInTask = new List<object[]>
		{
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.SuccessTask(10)), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.NotFoundTask<int>("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.NotFoundTask<int>(new [] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.ErrorTask<int>("1")), DomainOperationStatus.Error, new [] { "1" } },
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.ErrorTask<int>(new [] { "1", "2" })), DomainOperationStatus.Error, new [] { "1", "2" } },
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.ErrorTask<int>(new[] { new ValidationResult("1") })), DomainOperationStatus.Error, new [] { "1" } }
		};
		#endregion // Test of 'Task<(TValue, IDomainResult)>' responses -------
	}
}