using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using AK.DomainResults.Domain;

namespace AK.DomainResults.Examples.Domain
{
	public class TupleServiceSync
	{
		#region Success responses ---------------------------------------------

		public IDomainResult GetSuccess() => DomainResult.Success();
		public IDomainResult<int> GetSuccessWithNumericValue() => DomainResult.Success(10);
		public (int, IDomainResult) GetSuccessWithNumericValueTuple() => ValueResult.Success(10);

		public Task<IDomainResult> GetSuccessTask() => DomainResult.SuccessTask();
		public Task<IDomainResult<int>> GetSuccessWithNumericValueTask() => DomainResult.SuccessTask(10);
		public Task<(int, IDomainResult)> GetSuccessWithNumericValueTupleTask() => ValueResult.SuccessTask(10);

		#endregion // Success responses ---------------------------------------

		#region Not Found responses -------------------------------------------

		public IDomainResult GetNotFoundWithNoMessage() => DomainResult.NotFound();
		public IDomainResult GetNotFoundWithMessage() => DomainResult.NotFound("No, really not found");
		public IDomainResult GetNotFoundWithMessages() => DomainResult.NotFound(new[] { "No, really not found", "Searched everwhere" });

		public Task<IDomainResult> GetNotFoundWithNoMessageTask() => DomainResult.NotFoundTask();
		public Task<IDomainResult> GetNotFoundWithMessageTask() => DomainResult.NotFoundTask("No, really not found");
		public Task<IDomainResult> GetNotFoundWithMessagesTask() => DomainResult.NotFoundTask(new[] { "No, really not found", "Searched everwhere" });

		public IDomainResult<int> GetNotFoundWithNoMessageWhenExpectedNumber() => DomainResult.NotFound<int>();
		public IDomainResult<int> GetNotFoundWithMessageWhenExpectedNumber() => DomainResult.NotFound<int>("No, really not found");
		public IDomainResult<int> GetNotFoundWithMessagesWhenExpectedNumber() => DomainResult.NotFound<int>(new[] { "No, really not found", "Searched everwhere" });

		public Task<IDomainResult<int>> GetNotFoundWithNoMessageWhenExpectedNumberTask() => DomainResult.NotFoundTask<int>();
		public Task<IDomainResult<int>> GetNotFoundWithMessageWhenExpectedNumberTask() => DomainResult.NotFoundTask<int>("No, really not found");
		public Task<IDomainResult<int>> GetNotFoundWithMessagesWhenExpectedNumberTask() => DomainResult.NotFoundTask<int>(new[] { "No, really not found", "Searched everwhere" });

		public (int, IDomainResult) GetNotFoundWithNoMessageWhenExpectedNumberTuple() => ValueResult.NotFound<int>();
		public (int, IDomainResult) GetNotFoundWithMessageWhenExpectedNumberTuple() => ValueResult.NotFound<int>("No, really not found");
		public (int, IDomainResult) GetNotFoundWithMessagesWhenExpectedNumberTuple() => ValueResult.NotFound<int>(new[] { "No, really not found", "Searched everwhere" });

		public Task<(int, IDomainResult)> GetNotFoundWithNoMessageWhenExpectedNumberTupleTask() => ValueResult.NotFoundTask<int>();
		public Task<(int, IDomainResult)> GetNotFoundWithMessageWhenExpectedNumberTupleTask() => ValueResult.NotFoundTask<int>("No, really not found");
		public Task<(int, IDomainResult)> GetNotFoundWithMessagesWhenExpectedNumberTupleTask() => ValueResult.NotFoundTask<int>(new[] { "No, really not found", "Searched everwhere" });

		#endregion // Not Found responses -------------------------------------

		#region Error responses -----------------------------------------------

		public IDomainResult GetErrorWithNoMessage() => DomainResult.Error();
		public IDomainResult GetErrorWithMessage() => DomainResult.Error("Ah, error");
		public IDomainResult GetErrorWithMessages() => DomainResult.Error(new[] { "Ah, error", "Terrible error" });
		public IDomainResult GetErrorWithValidationMessages() =>
			DomainResult.Error(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<IDomainResult> GetErrorWithNoMessageTask() => DomainResult.ErrorTask();
		public Task<IDomainResult> GetErrorWithMessageTask() => DomainResult.ErrorTask("Ah, error");
		public Task<IDomainResult> GetErrorWithMessagesTask() => DomainResult.ErrorTask(new[] { "Ah, error", "Terrible error" });
		public Task<IDomainResult> GetErrorWithValidationMessagesTask() =>
			DomainResult.ErrorTask(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public IDomainResult<int> GetErrorWithNoMessageWhenExpectedNumber() => DomainResult.Error<int>();
		public IDomainResult<int> GetErrorWithMessageWhenExpectedNumber() => DomainResult.Error<int>("Ah, error");
		public IDomainResult<int> GetErrorWithMessagesWhenExpectedNumber() => DomainResult.Error<int>(new[] { "Ah, error", "Terrible error" });
		public IDomainResult<int> GetErrorWithValidationMessagesWhenExpectedNumber() =>
			DomainResult.Error<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<IDomainResult<int>> GetErrorWithNoMessageWhenExpectedNumberTask() => DomainResult.ErrorTask<int>();
		public Task<IDomainResult<int>> GetErrorWithMessageWhenExpectedNumberTask() => DomainResult.ErrorTask<int>("Ah, error");
		public Task<IDomainResult<int>> GetErrorWithMessagesWhenExpectedNumberTask() => DomainResult.ErrorTask<int>(new[] { "Ah, error", "Terrible error" });
		public Task<IDomainResult<int>> GetErrorWithValidationMessagesWhenExpectedNumberTask() =>
			DomainResult.ErrorTask<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public (int, IDomainResult) GetErrorWithNoMessageWhenExpectedNumberTuple() => ValueResult.Error<int>();
		public (int, IDomainResult) GetErrorWithMessageWhenExpectedNumberTuple() => ValueResult.Error<int>("Ah, error");
		public (int, IDomainResult) GetErrorWithMessagesWhenExpectedNumberTuple() => ValueResult.Error<int>(new[] { "Ah, error", "Terrible error" });
		public (int, IDomainResult) GetErrorWithValidationMessagesWhenExpectedNumberTuple() =>
			ValueResult.Error<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		public Task<(int, IDomainResult)> GetErrorWithNoMessageWhenExpectedNumberTupleTask() => ValueResult.ErrorTask<int>();
		public Task<(int, IDomainResult)> GetErrorWithMessageWhenExpectedNumberTupleTask() => ValueResult.ErrorTask<int>("Ah, error");
		public Task<(int, IDomainResult)> GetErrorWithMessagesWhenExpectedNumberTupleTask() => ValueResult.ErrorTask<int>(new[] { "Ah, error", "Terrible error" });
		public Task<(int, IDomainResult)> GetErrorWithValidationMessagesWhenExpectedNumberTupleTask() =>
			ValueResult.ErrorTask<int>(new[]
				{
					new ValidationResult("Validation message1", new[] { "fieldName1" }),
					new ValidationResult("Validation message2", new[] { "fieldName2" })
				});

		#endregion // Error responses -----------------------------------------
	}
}
