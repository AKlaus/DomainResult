using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DomainResults.Common;
using Xunit;

namespace DomainResults.Tests.Common
{
	public class IDomainResult_Value_Tests
	{
		#region Test of 'IDomainResult<TValue>' responses ---------------------
		[Fact]
		public void Implicitly_Convert_DomainResult_Test()
		{
			// If the code below gets complied, then implicit conversion works

			Func<int, DomainResult<int>> func = (i) => i;
			var res = func(10);
		}

		[Fact]
		public void DomainResult_Can_Be_Deconstructed_Test()
		{
			var res = DomainResult.Success(10);
			var (value, details) = res;
			
			Assert.Equal(10, value);
			Assert.IsAssignableFrom<IDomainResult>(details);
			Assert.Equal(DomainOperationStatus.Success, details.Status);
		}

		[Theory]
		[MemberData(nameof(TestCasesWithValue))]
		public void IDomainResult_Value_Response_Test(Func<IDomainResult<int>> method, DomainOperationStatus expectedStatus, IEnumerable<string> expectedErrMessages)
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

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.Unauthorized<int>("1")), DomainOperationStatus.Unauthorized, new [] { "1" } },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.Failed<int>("1")), DomainOperationStatus.Failed, new[] { "1" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.Failed<int>(new[] { "1", "2" })), DomainOperationStatus.Failed, new[] { "1", "2" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult.Failed<int>(new[] { new ValidationResult("1") })), DomainOperationStatus.Failed, new[] { "1" } },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Success(10)),  DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.NotFound("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.NotFound(new[] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Unauthorized("1")), DomainOperationStatus.Unauthorized, new [] { "1" } },

			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Failed("1")), DomainOperationStatus.Failed, new[] { "1" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Failed(new[] { "1", "2" })), DomainOperationStatus.Failed, new[] { "1", "2" } },
			new object[] { (Func<IDomainResult<int>>)(() => DomainResult<int>.Failed(new[] { new ValidationResult("1") })), DomainOperationStatus.Failed, new[] { "1" } }
		};
		#endregion // Test of 'IDomainResult<TValue>' responses ---------------

		#region Test of 'Task<IDomainResult<TValue>>' responses ---------------

		[Theory]
		[MemberData(nameof(TestCasesWithValueWrappedInTask))]
		public async Task Task_IDomainResult_Value_Response_Test(Func<Task<IDomainResult<int>>> method, DomainOperationStatus expectedStatus, IEnumerable<string> expectedErrMessages)
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

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.UnauthorizedTask<int>("1")), DomainOperationStatus.Unauthorized, new [] { "1" } },

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.FailedTask<int>("1")), DomainOperationStatus.Failed, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.FailedTask<int>(new [] { "1", "2" })), DomainOperationStatus.Failed, new [] { "1", "2" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult.FailedTask<int>(new[] { new ValidationResult("1") })), DomainOperationStatus.Failed, new [] { "1" } },
			
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.SuccessTask(10)), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.NotFoundTask("1")), DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.NotFoundTask(new [] { "1", "2" })), DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.UnauthorizedTask("1")), DomainOperationStatus.Unauthorized, new [] { "1" } },

			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.FailedTask("1")), DomainOperationStatus.Failed, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.FailedTask(new [] { "1", "2" })), DomainOperationStatus.Failed, new [] { "1", "2" } },
			new object[] { (Func<Task<IDomainResult<int>>>)(() => DomainResult<int>.FailedTask(new[] { new ValidationResult("1") })), DomainOperationStatus.Failed, new [] { "1" } }
		};
		#endregion // Test of 'Task<IDomainResult<TValue>>' responses ---------
	}
}
