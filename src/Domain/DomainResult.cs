using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AK.DomainResults.Domain
{
	public class DomainResult : IDomainResult
	{
		private readonly IDomainResult _status;

		public DomainOperationStatus Status			=> _status.Status;
		public IReadOnlyCollection<string> Errors	=> _status.Errors;
		public bool IsSuccess						=> _status.IsSuccess;

		private DomainResult()															: this(DomainStatus.Success()) { }
		private DomainResult(DomainOperationStatus status, string? error)				: this(new DomainStatus(status, !string.IsNullOrEmpty(error) ? new[] { error } : new string[0])) { }
		private DomainResult(DomainOperationStatus status, IEnumerable<string> errors)	: this(new DomainStatus(status, errors)) { }
		private DomainResult(IEnumerable<ValidationResult> errors)						: this(new DomainStatus(errors)) {}
		private DomainResult(IDomainResult errorDetails)
		{
			_status = errorDetails;
		}

		public static IDomainResult Success()							 => new DomainResult();
		public static IDomainResult NotFound(string? message = null)	 => new DomainResult(DomainOperationStatus.NotFound, message);
		public static IDomainResult NotFound(IEnumerable<string> messages)=> new DomainResult(DomainOperationStatus.NotFound, messages);
		public static IDomainResult Error(string? message = null)		 => new DomainResult(DomainOperationStatus.Error, message);
		public static IDomainResult Error(IEnumerable<string> errors)	 => new DomainResult(DomainOperationStatus.Error, errors);
		public static IDomainResult Error(IEnumerable<ValidationResult> validationResults) => new DomainResult(validationResults);

		public static Task<IDomainResult> SuccessTask()								=> Task.FromResult(Success());
		public static Task<IDomainResult> NotFoundTask(string? message = null)		=> Task.FromResult(NotFound(message));
		public static Task<IDomainResult> NotFoundTask(IEnumerable<string> messages)=> Task.FromResult(NotFound(messages));
		public static Task<IDomainResult> ErrorTask(string? message = null)			=> Task.FromResult(Error(message));
		public static Task<IDomainResult> ErrorTask(IEnumerable<string> errors)		=> Task.FromResult(Error(errors));
		public static Task<IDomainResult> ErrorTask(IEnumerable<ValidationResult> validationResults) => Task.FromResult(Error(validationResults));

		public static DomainResult<TValue> Success<TValue>(TValue value)					=> DomainResult<TValue>.Success(value);
		public static DomainResult<TValue> NotFound<TValue>(string? message = null)			=> DomainResult<TValue>.NotFound(message);
		public static DomainResult<TValue> NotFound<TValue>(IEnumerable<string> messages)	=> DomainResult<TValue>.NotFound(messages);
		public static DomainResult<TValue> Error<TValue>(string? message = null)			=> DomainResult<TValue>.Error(message);
		public static DomainResult<TValue> Error<TValue>(IEnumerable<string> errors)		=> DomainResult<TValue>.Error(errors);
		public static DomainResult<TValue> Error<TValue>(IEnumerable<ValidationResult> validationResults) => DomainResult<TValue>.Error(validationResults);

		public static Task<IDomainResult<TValue>> SuccessTask<TValue>(TValue value)					=> Task.FromResult(DomainResult<TValue>.Success(value) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(string? message = null)		=> Task.FromResult(DomainResult<TValue>.NotFound(message) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(IEnumerable<string> messages)=> Task.FromResult(DomainResult<TValue>.NotFound(messages) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask<TValue>(string? message = null)			=> Task.FromResult(DomainResult<TValue>.Error(message) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask<TValue>(IEnumerable<string> errors)		=> Task.FromResult(DomainResult<TValue>.Error(errors) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask<TValue>(IEnumerable<ValidationResult> validationResults) => Task.FromResult(DomainResult<TValue>.Error(validationResults) as IDomainResult<TValue>);
	}
}