using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AK.DomainResults.Domain
{
	public class ErrorDetails : IDomainResult
	{
		public DomainOperationStatus Status { get; }
		public IReadOnlyCollection<string> Errors { get; }

		public bool IsSuccess => Status == DomainOperationStatus.Success;

		public ErrorDetails() : this(DomainOperationStatus.Success, new string[0]) { }
		public ErrorDetails(DomainOperationStatus status, IEnumerable<string> errors)
		{
			Status = status;
			Errors = errors.ToArray();
		}
		public ErrorDetails(IEnumerable<ValidationResult> validationResults)
		{
			Status = DomainOperationStatus.Error;
			Errors = (from message in validationResults
					  select $"{message.ErrorMessage}{(message.MemberNames?.Any() == true ? " (" + string.Join(", ", message.MemberNames) + ")" : "")}"
					 ).ToArray();
		}

		public static IDomainResult Success() => new ErrorDetails();
		public static IDomainResult NotFound(IEnumerable<string> errors) => new ErrorDetails(DomainOperationStatus.NotFound, errors);
		public static IDomainResult Error(IEnumerable<string> errors)	 => new ErrorDetails(DomainOperationStatus.Error, errors);
		public static IDomainResult Error(IEnumerable<ValidationResult> validationResults) => new ErrorDetails(validationResults);
	}
}