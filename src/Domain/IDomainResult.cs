using System.Collections.Generic;

namespace AK.DomainResults.Domain
{
	public interface IDomainResult
	{
		IReadOnlyCollection<string> Errors { get; }
		bool IsSuccess { get; }
		DomainOperationStatus Status { get; }
	}

	public interface IDomainResult<TValue> : IDomainResult
	{
		TValue Value { get; }
	}
}