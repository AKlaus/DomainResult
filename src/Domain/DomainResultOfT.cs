using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AK.DomainResults.Domain
{
	public class DomainResult<TValue> : IDomainResult<TValue>
	{
		private readonly DomainStatus _status;

		public DomainOperationStatus Status			=> _status.Status;
		public IReadOnlyCollection<string> Errors	=> _status.Errors;
		public bool IsSuccess						=> _status.IsSuccess;

		public TValue Value { get; }

		private DomainResult(DomainOperationStatus status, string? error)				: this(status, (!string.IsNullOrEmpty(error) ? new[] { error } : new string[0])!) { }
		private DomainResult(DomainOperationStatus status, IEnumerable<string> errors)	: this(new DomainStatus(status, errors)) { }
		private DomainResult(DomainStatus errorDetails)									: this(default!, errorDetails) { }
		private DomainResult(TValue value)												: this(value, new DomainStatus()) {}
		private DomainResult(TValue value, DomainStatus errorDetails)
		{
			Value = value;
			_status = errorDetails;
		}

		public static DomainResult<TValue> Success(TValue value)					=> new DomainResult<TValue>(value);
		public static DomainResult<TValue> NotFound(string? message = null)			=> new DomainResult<TValue>(DomainOperationStatus.NotFound, message);
		public static DomainResult<TValue> NotFound(IEnumerable<string> messages)	=> new DomainResult<TValue>(DomainOperationStatus.NotFound, messages);
		public static DomainResult<TValue> Error(string? message = null)			=> new DomainResult<TValue>(DomainOperationStatus.Error, message);
		public static DomainResult<TValue> Error(IEnumerable<string> errors)		=> new DomainResult<TValue>(DomainOperationStatus.Error, errors);
		public static DomainResult<TValue> Error(IEnumerable<ValidationResult> validationResults) => new DomainResult<TValue>(new DomainStatus(validationResults));

		public static Task<IDomainResult<TValue>> SuccessTask(TValue value)					=> Task.FromResult(Success(value) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> NotFoundTask(string? message = null)		=> Task.FromResult(NotFound(message) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> NotFoundTask(IEnumerable<string> messages)=> Task.FromResult(NotFound(messages) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(string? message = null)			=> Task.FromResult(Error(message) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(IEnumerable<string> errors)		=> Task.FromResult(Error(errors) as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(IEnumerable<ValidationResult> validationResults)
																							=> Task.FromResult(Error(validationResults) as IDomainResult<TValue>);
	}
}
