using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using DomainResults.Domain;

using Xunit;

namespace DomainResults.Domain.Tests
{
	public class IDomainResultValueTests
	{
		#region Test of 'IDomainResult<TValue>' responses ---------------------

		[Theory]
		[MemberData(nameof(TestCasesWithValue))]
		public void IDomainResultValueResponseTest(Func<IDomainResult<int>> method, DomainOperationStatus expectedStatus, IEnumerable<string> expectedErrMessages)
		{
			var domainResult = method();

			if (expectedStatus == DomainOperationStatus.Success)
			{
				Assert.True(domainResult.IsSuccess);
				Assert.True(domainResult.Value > 0);
			}

			Assert.Equal(expectedStatus, domainResult.Status);
			Assert.Equal(expectedErrMessages, domainResult.Errors);
		}

		public static IEnumerable<object[]> TestCasesWithValue
		{
			get
			{	foreach (var t in testCasesWithValue)
					yield return t;
			}
		}

		private static readonly IEnumerable<object[]> testCasesWithValue = new List<object[]>
		{
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.Success(10)),  DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.NotFound<int>("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.NotFound<int>(new[] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.Error<int>("1")), DomainOperationStatus.Error, new[] { "1" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.Error<int>(new[] { "1", "2" })), DomainOperationStatus.Error, new[] { "1", "2" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.Error<int>(new[] { new ValidationResult("1") })), DomainOperationStatus.Error, new[] { "1" } },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Success(10)),  DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.NotFound("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.NotFound(new[] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Error("1")), DomainOperationStatus.Error, new[] { "1" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Error(new[] { "1", "2" })), DomainOperationStatus.Error, new[] { "1", "2" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Error(new[] { new ValidationResult("1") })), DomainOperationStatus.Error, new[] { "1" } }
		};
		#endregion // Test of 'IDomainResult<TValue>' responses ---------------

		#region Test of 'Task<IDomainResult<TValue>>' responses ---------------

		[Theory]
		[MemberData(nameof(TestCasesWithValueWrappedInTask))]
		public async Task TaskIDomainResultValueResponseTest(Func<Task<IDomainResult<int>>> method, DomainOperationStatus expectedStatus, IEnumerable<string> expectedErrMessages)
		{
			var domainResult = await method();

			if (expectedStatus == DomainOperationStatus.Success)
			{
				Assert.True(domainResult.IsSuccess);
				Assert.True(domainResult.Value > 0);
			}

			Assert.Equal(expectedStatus, domainResult.Status);
			Assert.Equal(expectedErrMessages, domainResult.Errors);
		}

		public static IEnumerable<object[]> TestCasesWithValueWrappedInTask
		{
			get
			{
				foreach (var t in testCasesWithValueWrappedInTask)
					yield return t;
			}
		}

		private static readonly IEnumerable<object[]> testCasesWithValueWrappedInTask = new List<object[]>
		{
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.SuccessTask(10)), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.NotFoundTask<int>("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.NotFoundTask<int>(new [] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.ErrorTask<int>("1")), DomainOperationStatus.Error, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.ErrorTask<int>(new [] { "1", "2" })), DomainOperationStatus.Error, new [] { "1", "2" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.ErrorTask<int>(new[] { new ValidationResult("1") })), DomainOperationStatus.Error, new [] { "1" } },
			
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.SuccessTask(10)), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.NotFoundTask("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.NotFoundTask(new [] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.ErrorTask("1")), DomainOperationStatus.Error, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.ErrorTask(new [] { "1", "2" })), DomainOperationStatus.Error, new [] { "1", "2" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.ErrorTask(new[] { new ValidationResult("1") })), DomainOperationStatus.Error, new [] { "1" } }
		};
		#endregion // Test of 'Task<IDomainResult<TValue>>' responses ---------
	}
}
