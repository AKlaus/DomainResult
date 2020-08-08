using System.Collections.Generic;

namespace AK.DomainResults.Domain
{
	public interface IDomainResultBase
	{
		IReadOnlyCollection<string> Errors { get; }
		bool IsSuccess { get; }
		DomainOperationStatus Status { get; }
	}

	public interface IDomainResult: IDomainResultBase	{}

	public interface IDomainResult<TValue> : IDomainResultBase
	{
		TValue Value { get; }
	}
}