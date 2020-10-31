using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using DomainResults.Common;

namespace DomainResults.Examples.Domain
{
	public class DomainFailedService
	{
		public IDomainResult GetFailedWithNoMessage()=> DomainResult.Failed();
		public IDomainResult GetFailedWithMessage()	=> DomainResult.Failed("Ah, error");
		public IDomainResult GetFailedWithMessages() => DomainResult.Failed(new[] { "Ah, error", "Terrible error" });
		public IDomainResult GetErrorWithValidationMessages() =>
			DomainResult.Failed(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<IDomainResult> GetFailedWithNoMessageTask()	=> DomainResult.FailedTask();
		public Task<IDomainResult> GetFailedWithMessageTask()	=> DomainResult.FailedTask("Ah, error");
		public Task<IDomainResult> GetFailedWithMessagesTask()	=> DomainResult.FailedTask(new[] { "Ah, error", "Terrible error" });
		public Task<IDomainResult> GetFailedWithValidationMessagesTask() =>
			DomainResult.FailedTask(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public IDomainResult<int> GetFailedWithNoMessageWhenExpectedNumber()	=> DomainResult.Failed<int>();
		public IDomainResult<int> GetFailedWithMessageWhenExpectedNumber()	=> DomainResult.Failed<int>("Ah, error");
		public IDomainResult<int> GetFailedWithMessagesWhenExpectedNumber()	=> DomainResult.Failed<int>(new[] { "Ah, error", "Terrible error" });
		public IDomainResult<int> GetErrorWithValidationMessagesWhenExpectedNumber() =>
			DomainResult.Failed<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<IDomainResult<int>> GetFailedWithNoMessageWhenExpectedNumberTask()=> DomainResult.FailedTask<int>();
		public Task<IDomainResult<int>> GetFailedWithMessageWhenExpectedNumberTask()	 => DomainResult.FailedTask<int>("Ah, error");
		public Task<IDomainResult<int>> GetFailedWithMessagesWhenExpectedNumberTask() => DomainResult.FailedTask<int>(new[] { "Ah, error", "Terrible error" });
		public Task<IDomainResult<int>> GetFailedWithValidationMessagesWhenExpectedNumberTask() =>
			DomainResult.FailedTask<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public (int, IDomainResult) GetFailedWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.Failed<int>();
		public (int, IDomainResult) GetFailedWithMessageWhenExpectedNumberTuple()	=> IDomainResult.Failed<int>("Ah, error");
		public (int, IDomainResult) GetFailedWithMessagesWhenExpectedNumberTuple()	=> IDomainResult.Failed<int>(new[] { "Ah, error", "Terrible error" });
		public (int, IDomainResult) GetFailedWithValidationMessagesWhenExpectedNumberTuple() =>
			IDomainResult.Failed<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<(int, IDomainResult)> GetFailedWithNoMessageWhenExpectedNumberTupleTask()=> IDomainResult.FailedTask<int>();
		public Task<(int, IDomainResult)> GetFailedWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.FailedTask<int>("Ah, error");
		public Task<(int, IDomainResult)> GetFailedWithMessagesWhenExpectedNumberTupleTask() => IDomainResult.FailedTask<int>(new[] { "Ah, error", "Terrible error" });
		public Task<(int, IDomainResult)> GetFailedWithValidationMessagesWhenExpectedNumberTupleTask() =>
			IDomainResult.FailedTask<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});
	}
}
