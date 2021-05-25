#if !NETCOREAPP2_0 && !NETCOREAPP2_1
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using DomainResults.Common;

using Xunit;

namespace DomainResults.Tests.Common
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
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
			{	// ReSharper disable once LoopCanBeConvertedToQuery as the 'yield return' is vital to do the trick
				foreach (var t in testCases)
					yield return t;
			}
		}

		private static readonly IEnumerable<object[]> testCases = new List<object[]>
		{
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Success(10)), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.NotFound<int>("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.NotFound<int>(new[] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Unauthorized<int>()), DomainOperationStatus.Unauthorized, new string[0] },
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Unauthorized<int>("1")), DomainOperationStatus.Unauthorized, new [] { "1" } },

			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Failed<int>("1")), DomainOperationStatus.Failed, new[] { "1" } },
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Failed<int>(new[] { "1", "2" })), DomainOperationStatus.Failed, new[] { "1", "2" } },
			new object[] { (Func<(int, IDomainResult)>)(() => IDomainResult.Failed<int>(new[] { new ValidationResult("1") })), DomainOperationStatus.Failed, new[] { "1" } }
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
			{	// ReSharper disable once LoopCanBeConvertedToQuery as the 'yield return' is vital to do the trick
				foreach (var t in testCasesWrappedInTask)
					yield return t;
			}
		}

		private static readonly IEnumerable<object[]> testCasesWrappedInTask = new List<object[]>
		{
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.SuccessTask(10)), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.NotFoundTask<int>("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.NotFoundTask<int>(new [] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.UnauthorizedTask<int>()), DomainOperationStatus.Unauthorized, new string[0] },
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.UnauthorizedTask<int>("1")), DomainOperationStatus.Unauthorized, new [] { "1" } },

			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.FailedTask<int>("1")), DomainOperationStatus.Failed, new [] { "1" } },
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.FailedTask<int>(new [] { "1", "2" })), DomainOperationStatus.Failed, new [] { "1", "2" } },
			new object[] { (Func<Task<(int, IDomainResult)>>)(() => IDomainResult.FailedTask<int>(new[] { new ValidationResult("1") })), DomainOperationStatus.Failed, new [] { "1" } }
		};
		#endregion // Test of 'Task<(TValue, IDomainResult)>' responses -------
	}
}
#endif
