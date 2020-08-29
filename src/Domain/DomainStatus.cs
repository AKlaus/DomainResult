using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AK.DomainResults.Domain
{
	public class DomainStatus: IDomainResult
	{
		public DomainOperationStatus Status { get; }
		public IReadOnlyCollection<string> Errors { get; }

		public bool IsSuccess => Status == DomainOperationStatus.Success;

		internal DomainStatus() : this(DomainOperationStatus.Success, new string[0]) { }
		internal DomainStatus(DomainOperationStatus status, IEnumerable<string> errors)
		{
			Status = status;
			Errors = errors.ToArray();
		}
		internal DomainStatus(IEnumerable<ValidationResult> validationResults)
		{
			Status = DomainOperationStatus.Error;
			Errors = (from message in validationResults
					  select $"{message.ErrorMessage}{(message.MemberNames?.Any() == true ? " (" + string.Join(", ", message.MemberNames) + ")" : "")}"
					 ).ToArray();
		}

		public static IDomainResult Success()							  => new DomainStatus();
		public static IDomainResult NotFound(string? message = null)	  => NotFound(!string.IsNullOrEmpty(message) ? new[] { message } : new string[0]);
		public static IDomainResult NotFound(IEnumerable<string> messages)=> new DomainStatus(DomainOperationStatus.NotFound, messages);
		public static IDomainResult Error(string? error = null)			  => Error(!string.IsNullOrEmpty(error) ? new[] { error } : new string[0]);
		public static IDomainResult Error(IEnumerable<string> errors)	  => new DomainStatus(DomainOperationStatus.Error, errors);
		public static IDomainResult Error(IEnumerable<ValidationResult> validationResults) => new DomainStatus(validationResults);
	}
}