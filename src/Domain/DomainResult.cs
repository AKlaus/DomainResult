using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AK.DomainResults.Domain
{
	public class DomainResult : ErrorDetails
	{
		public DomainResult() : base() { }
		public DomainResult(DomainOperationStatus status, string? error) : base(status, (!string.IsNullOrEmpty(error) ? new[] { error } : new string[0])!) { }
		public DomainResult(DomainOperationStatus status, IEnumerable<string> errors) : base(status, errors) { }
		public DomainResult(IEnumerable<ValidationResult> errors) : base(errors) { }

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

	public class DomainResult<TValue> : Tuple<TValue, IDomainResult>, IDomainResult<TValue>
	{
		public DomainOperationStatus Status => Item2.Status;
		public IReadOnlyCollection<string> Errors => Item2.Errors;
		public TValue Value => Item1;

		public bool IsSuccess => Status == DomainOperationStatus.Success;

		public DomainResult(DomainOperationStatus status, string? error) : this(status, (!string.IsNullOrEmpty(error) ? new[] { error } : new string[0])!) { }
		public DomainResult(DomainOperationStatus status, IEnumerable<string> errors) : this(new ErrorDetails(status, errors)) { }
		public DomainResult(ErrorDetails errorDetails) : base(default!, errorDetails) { }
		public DomainResult(TValue value) : base(value, new ErrorDetails()) { }

		public static DomainResult<TValue> Success(TValue value)					=> new DomainResult<TValue>(value);
		public static DomainResult<TValue> NotFound(string? message = null)			=> new DomainResult<TValue>(DomainOperationStatus.NotFound, message);
		public static DomainResult<TValue> NotFound(IEnumerable<string> messages)	=> new DomainResult<TValue>(DomainOperationStatus.NotFound, messages);
		public static DomainResult<TValue> Error(string? message = null)			=> new DomainResult<TValue>(DomainOperationStatus.Error, message);
		public static DomainResult<TValue> Error(IEnumerable<string> errors)		=> new DomainResult<TValue>(DomainOperationStatus.Error, errors);
		public static DomainResult<TValue> Error(IEnumerable<ValidationResult> validationResults) => new DomainResult<TValue>(new ErrorDetails(validationResults));

		public static Task<IDomainResult<TValue>> SuccessTask(TValue value)					=> Task.FromResult(Success(value) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> NotFoundTask(string? message = null)		=> Task.FromResult(NotFound(message) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> NotFoundTask(IEnumerable<string> messages)=> Task.FromResult(NotFound(messages) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(string? message = null)			=> Task.FromResult(Error(message) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(IEnumerable<string> errors)		=> Task.FromResult(Error(errors) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(IEnumerable<ValidationResult> validationResults)
																							=> Task.FromResult(Error(validationResults) as IDomainResult<TValue>);
	}
}