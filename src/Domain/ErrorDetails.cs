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
					  select $"{message.ErrorMessage} {(message.MemberNames != null ? "(" + string.Join(", ", message.MemberNames) + ")" : "")})"
					 ).ToArray();
		}
	}
}