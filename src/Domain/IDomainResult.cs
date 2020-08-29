using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AK.DomainResults.Domain
{
	public interface IDomainResultBase
	{
		IReadOnlyCollection<string> Errors { get; }
		bool IsSuccess { get; }
		DomainOperationStatus Status { get; }
	}

	public interface IDomainResult: IDomainResultBase
	{
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1
		static IDomainResult Success()								=> DomainResult.Success();
		static IDomainResult NotFound(string? message = null)		=> DomainResult.NotFound(message);
		static IDomainResult NotFound(IEnumerable<string> messages) => DomainResult.NotFound(messages);
		static IDomainResult Error(string? error = null)			=> DomainResult.Error(error);
		static IDomainResult Error(IEnumerable<string> errors)		=> DomainResult.Error(errors);
		static IDomainResult Error(IEnumerable<ValidationResult> validationResults) => DomainResult.Error(validationResults);
#endif
	}

	public interface IDomainResult<TValue> : IDomainResultBase
	{
		TValue Value { get; }
	}
}