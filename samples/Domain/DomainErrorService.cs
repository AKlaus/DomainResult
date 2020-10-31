using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using DomainResults.Common;

namespace DomainResults.Examples.Domain
{
	public class DomainErrorService
	{
		public IDomainResult GetErrorWithNoMessage()=> DomainResult.Failed();
		public IDomainResult GetErrorWithMessage()	=> DomainResult.Failed("Ah, error");
		public IDomainResult GetErrorWithMessages() => DomainResult.Failed(new[] { "Ah, error", "Terrible error" });
		public IDomainResult GetErrorWithValidationMessages() =>
			DomainResult.Failed(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<IDomainResult> GetErrorWithNoMessageTask()	=> DomainResult.FailedTask();
		public Task<IDomainResult> GetErrorWithMessageTask()	=> DomainResult.FailedTask("Ah, error");
		public Task<IDomainResult> GetErrorWithMessagesTask()	=> DomainResult.FailedTask(new[] { "Ah, error", "Terrible error" });
		public Task<IDomainResult> GetErrorWithValidationMessagesTask() =>
			DomainResult.FailedTask(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public IDomainResult<int> GetErrorWithNoMessageWhenExpectedNumber()	=> DomainResult.Failed<int>();
		public IDomainResult<int> GetErrorWithMessageWhenExpectedNumber()	=> DomainResult.Failed<int>("Ah, error");
		public IDomainResult<int> GetErrorWithMessagesWhenExpectedNumber()	=> DomainResult.Failed<int>(new[] { "Ah, error", "Terrible error" });
		public IDomainResult<int> GetErrorWithValidationMessagesWhenExpectedNumber() =>
			DomainResult.Failed<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<IDomainResult<int>> GetErrorWithNoMessageWhenExpectedNumberTask()=> DomainResult.FailedTask<int>();
		public Task<IDomainResult<int>> GetErrorWithMessageWhenExpectedNumberTask()	 => DomainResult.FailedTask<int>("Ah, error");
		public Task<IDomainResult<int>> GetErrorWithMessagesWhenExpectedNumberTask() => DomainResult.FailedTask<int>(new[] { "Ah, error", "Terrible error" });
		public Task<IDomainResult<int>> GetErrorWithValidationMessagesWhenExpectedNumberTask() =>
			DomainResult.FailedTask<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public (int, IDomainResult) GetErrorWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.Failed<int>();
		public (int, IDomainResult) GetErrorWithMessageWhenExpectedNumberTuple()	=> IDomainResult.Failed<int>("Ah, error");
		public (int, IDomainResult) GetErrorWithMessagesWhenExpectedNumberTuple()	=> IDomainResult.Failed<int>(new[] { "Ah, error", "Terrible error" });
		public (int, IDomainResult) GetErrorWithValidationMessagesWhenExpectedNumberTuple() =>
			IDomainResult.Failed<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<(int, IDomainResult)> GetErrorWithNoMessageWhenExpectedNumberTupleTask()=> IDomainResult.FailedTask<int>();
		public Task<(int, IDomainResult)> GetErrorWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.FailedTask<int>("Ah, error");
		public Task<(int, IDomainResult)> GetErrorWithMessagesWhenExpectedNumberTupleTask() => IDomainResult.FailedTask<int>(new[] { "Ah, error", "Terrible error" });
		public Task<(int, IDomainResult)> GetErrorWithValidationMessagesWhenExpectedNumberTupleTask() =>
			IDomainResult.FailedTask<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});
	}
}
