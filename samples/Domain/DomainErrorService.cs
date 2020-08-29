using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using DomainResults.Domain;

namespace DomainResults.Examples.Domain
{
	public class DomainErrorService
	{
		public IDomainResult GetErrorWithNoMessage()=> DomainResult.Error();
		public IDomainResult GetErrorWithMessage()	=> DomainResult.Error("Ah, error");
		public IDomainResult GetErrorWithMessages() => DomainResult.Error(new[] { "Ah, error", "Terrible error" });
		public IDomainResult GetErrorWithValidationMessages() =>
			DomainResult.Error(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<IDomainResult> GetErrorWithNoMessageTask()	=> DomainResult.ErrorTask();
		public Task<IDomainResult> GetErrorWithMessageTask()	=> DomainResult.ErrorTask("Ah, error");
		public Task<IDomainResult> GetErrorWithMessagesTask()	=> DomainResult.ErrorTask(new[] { "Ah, error", "Terrible error" });
		public Task<IDomainResult> GetErrorWithValidationMessagesTask() =>
			DomainResult.ErrorTask(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public IDomainResult<int> GetErrorWithNoMessageWhenExpectedNumber()	=> DomainResult.Error<int>();
		public IDomainResult<int> GetErrorWithMessageWhenExpectedNumber()	=> DomainResult.Error<int>("Ah, error");
		public IDomainResult<int> GetErrorWithMessagesWhenExpectedNumber()	=> DomainResult.Error<int>(new[] { "Ah, error", "Terrible error" });
		public IDomainResult<int> GetErrorWithValidationMessagesWhenExpectedNumber() =>
			DomainResult.Error<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<IDomainResult<int>> GetErrorWithNoMessageWhenExpectedNumberTask()=> DomainResult.ErrorTask<int>();
		public Task<IDomainResult<int>> GetErrorWithMessageWhenExpectedNumberTask()	 => DomainResult.ErrorTask<int>("Ah, error");
		public Task<IDomainResult<int>> GetErrorWithMessagesWhenExpectedNumberTask() => DomainResult.ErrorTask<int>(new[] { "Ah, error", "Terrible error" });
		public Task<IDomainResult<int>> GetErrorWithValidationMessagesWhenExpectedNumberTask() =>
			DomainResult.ErrorTask<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public (int, IDomainResult) GetErrorWithNoMessageWhenExpectedNumberTuple()	=> ValueResult.Error<int>();
		public (int, IDomainResult) GetErrorWithMessageWhenExpectedNumberTuple()	=> ValueResult.Error<int>("Ah, error");
		public (int, IDomainResult) GetErrorWithMessagesWhenExpectedNumberTuple()	=> ValueResult.Error<int>(new[] { "Ah, error", "Terrible error" });
		public (int, IDomainResult) GetErrorWithValidationMessagesWhenExpectedNumberTuple() =>
			ValueResult.Error<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<(int, IDomainResult)> GetErrorWithNoMessageWhenExpectedNumberTupleTask()=> ValueResult.ErrorTask<int>();
		public Task<(int, IDomainResult)> GetErrorWithMessageWhenExpectedNumberTupleTask()	=> ValueResult.ErrorTask<int>("Ah, error");
		public Task<(int, IDomainResult)> GetErrorWithMessagesWhenExpectedNumberTupleTask() => ValueResult.ErrorTask<int>(new[] { "Ah, error", "Terrible error" });
		public Task<(int, IDomainResult)> GetErrorWithValidationMessagesWhenExpectedNumberTupleTask() =>
			ValueResult.ErrorTask<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});
	}
}
