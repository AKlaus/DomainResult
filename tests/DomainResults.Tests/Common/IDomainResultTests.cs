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
	public class IDomainResult_Tests
	{
		#region Test of 'IDomainResult' responses -----------------------------

		[Theory]
		[MemberData(nameof(TestCasesWithNoValue))]
		public void IDomainResult_Response_Test(Func<IDomainResult> method, DomainOperationStatus expectedStatus, IEnumerable<string> expectedErrMessages)
		{
			var domainResult = method();

			if (expectedStatus == DomainOperationStatus.Success)
				Assert.True(domainResult.IsSuccess);

			Assert.Equal(expectedStatus, domainResult.Status);
			Assert.Equal(expectedErrMessages, domainResult.Errors);
		}

		public static IEnumerable<object[]> TestCasesWithNoValue
		{
			get
			{	foreach (var t in testCasesWithNoValue)
					yield return t;
			}
		}

		private static readonly IEnumerable<object[]> testCasesWithNoValue = new List<object[]>
		{
			new object[] { (Func<IDomainResult>)(DomainResult.Success), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<IDomainResult>)(() => DomainResult.NotFound("1")),			DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<IDomainResult>)(() =>IDomainResult.NotFound(new[] { "1", "2" })),	DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<IDomainResult>)(() => DomainResult.Unauthorized()),	DomainOperationStatus.Unauthorized, new string[0] },
			new object[] { (Func<IDomainResult>)(() =>IDomainResult.Unauthorized("1")),	DomainOperationStatus.Unauthorized, new [] { "1" } },

			new object[] { (Func<IDomainResult>)(() => DomainResult.Failed("1")), DomainOperationStatus.Failed, new[] { "1" } },
			new object[] { (Func<IDomainResult>)(() => DomainResult.Failed(new[] { "1", "2" })), DomainOperationStatus.Failed, new[] { "1", "2" } },
			new object[] { (Func<IDomainResult>)(() =>IDomainResult.Failed(new[] { new ValidationResult("1") })), DomainOperationStatus.Failed, new[] { "1" } },
			
			new object[] { (Func<IDomainResult>)(() => DomainResult.CriticalDependencyError()),		DomainOperationStatus.CriticalDependencyError, new string[0] },
			new object[] { (Func<IDomainResult>)(() =>IDomainResult.CriticalDependencyError("1")),	DomainOperationStatus.CriticalDependencyError, new [] { "1" } }
		};
		#endregion // Test of 'IDomainResult' responses -----------------------

		#region Test of 'Task<IDomainResult>' responses -----------------------

		[Theory]
		[MemberData(nameof(TestCasesWithNoValueWrappedInTask))]
		public async Task Task_IDomainResult_Response_Test(Func<Task<IDomainResult>> method, DomainOperationStatus expectedStatus, IEnumerable<string> expectedErrMessages)
		{
			var domainResult = await method();

			if (expectedStatus == DomainOperationStatus.Success)
				Assert.True(domainResult.IsSuccess);

			Assert.Equal(expectedStatus, domainResult.Status);
			Assert.Equal(expectedErrMessages, domainResult.Errors);
		}

		public static IEnumerable<object[]> TestCasesWithNoValueWrappedInTask
		{
			get
			{
				foreach (var t in testCasesWithNoValueWrappedInTask)
					yield return t;
			}
		}

		private static readonly IEnumerable<object[]> testCasesWithNoValueWrappedInTask = new List<object[]>
		{
			new object[] { (Func<Task<IDomainResult>>)(DomainResult.SuccessTask), DomainOperationStatus.Success, new string[0] },

			new object[] { (Func<Task<IDomainResult>>)(() => DomainResult.NotFoundTask("1")),					DomainOperationStatus.NotFound, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult>>)(() =>IDomainResult.NotFoundTask(new [] { "1", "2" })),	DomainOperationStatus.NotFound, new [] { "1", "2" } },

			new object[] { (Func<Task<IDomainResult>>)(() => DomainResult.UnauthorizedTask()),				DomainOperationStatus.Unauthorized, new string[0] },
			new object[] { (Func<Task<IDomainResult>>)(() =>IDomainResult.UnauthorizedTask("1")),	DomainOperationStatus.Unauthorized, new [] { "1" } },

			new object[] { (Func<Task<IDomainResult>>)(() => DomainResult.FailedTask("1")), DomainOperationStatus.Failed, new [] { "1" } },
			new object[] { (Func<Task<IDomainResult>>)(() => DomainResult.FailedTask(new [] { "1", "2" })), DomainOperationStatus.Failed, new [] { "1", "2" } },
			new object[] { (Func<Task<IDomainResult>>)(() =>IDomainResult.FailedTask(new[] { new ValidationResult("1") })), DomainOperationStatus.Failed, new [] { "1" } },

			new object[] { (Func<Task<IDomainResult>>)(() => DomainResult.CriticalDependencyErrorTask()),		DomainOperationStatus.CriticalDependencyError, new string[0] },
			new object[] { (Func<Task<IDomainResult>>)(() =>IDomainResult.CriticalDependencyErrorTask("1")),	DomainOperationStatus.CriticalDependencyError, new [] { "1" } }
		};
		#endregion // Test of 'Task<IDomainResult>' responses -----------------
	}
}
