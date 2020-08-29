using System.Collections.Generic;

namespace DomainResults.Domain
{
	public interface IDomainResultBase
	{
		IReadOnlyCollection<string> Errors { get; }
		bool IsSuccess { get; }
		DomainOperationStatus Status { get; }
	}
}
