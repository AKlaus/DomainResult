using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DomainResults.Domain
{
	public class DomainResult<TValue> : IDomainResult<TValue>
	{
		private readonly IDomainResult _status;

		public DomainOperationStatus Status			=> _status.Status;
		public IReadOnlyCollection<string> Errors	=> _status.Errors;
		public bool IsSuccess						=> _status.IsSuccess;

		public TValue Value { get; }

		protected DomainResult(IDomainResult errorDetails)				: this(default!, errorDetails) { }
		protected DomainResult(TValue value)							: this(value, DomainResult.Success()) {}
		protected DomainResult(TValue value, IDomainResult errorDetails)
		{
			Value = value;
			_status = errorDetails;
		}

		public static IDomainResult<TValue> Success(TValue value)					=> new DomainResult<TValue>(value);
		public static IDomainResult<TValue> NotFound(string? message = null)		=> new DomainResult<TValue>(DomainResult.NotFound(message));
		public static IDomainResult<TValue> NotFound(IEnumerable<string> messages)	=> new DomainResult<TValue>(DomainResult.NotFound(messages));
		public static IDomainResult<TValue> Error(string? message = null)			=> new DomainResult<TValue>(DomainResult.Error(message));
		public static IDomainResult<TValue> Error(IEnumerable<string> errors)		=> new DomainResult<TValue>(DomainResult.Error(errors));
		public static IDomainResult<TValue> Error(IEnumerable<ValidationResult> validationResults) => new DomainResult<TValue>(DomainResult.Error(validationResults));

		public static Task<IDomainResult<TValue>> SuccessTask(TValue value)					=> Task.FromResult(Success(value)		as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> NotFoundTask(string? message = null)		=> Task.FromResult(NotFound(message)	as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> NotFoundTask(IEnumerable<string> messages)=> Task.FromResult(NotFound(messages)	as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(string? message = null)			=> Task.FromResult(Error(message)		as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(IEnumerable<string> errors)		=> Task.FromResult(Error(errors)		as IDomainResult<TValue>);
		public static Task<IDomainResult<TValue>> ErrorTask(IEnumerable<ValidationResult> validationResults)
																							=> Task.FromResult(Error(validationResults) as IDomainResult<TValue>);
	}
}
