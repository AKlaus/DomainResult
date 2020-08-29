using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DomainResults.Domain
{
	public partial class DomainResult : IDomainResult
	{
		public DomainOperationStatus Status { get; }
		public IReadOnlyCollection<string> Errors { get; }
		public bool IsSuccess => Status == DomainOperationStatus.Success;

		protected DomainResult()														: this(DomainOperationStatus.Success, string.Empty) { }
		protected DomainResult(DomainOperationStatus status, string? error)				: this(status, (!string.IsNullOrEmpty(error) ? new[] { error } : new string[0])!) { }
		public DomainResult(DomainOperationStatus status, IEnumerable<string> errors)
		{
			Status = status;
			Errors = errors.ToArray();
		}
		protected DomainResult(IEnumerable<ValidationResult> validationResults)
		{
			Status = DomainOperationStatus.Error;
			Errors = (from message in validationResults
					  select $"{message.ErrorMessage}{(message.MemberNames?.Any() == true ? " (" + string.Join(", ", message.MemberNames) + ")" : "")}"
					 ).ToArray();
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
	}
}