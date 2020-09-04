using System.Collections.Generic;

namespace DomainResults.Common
{
	/// <summary>
	///		Base interface for <see cref="IDomainResult"/> and <see cref="IDomainResult{T}"/>
	/// </summary>
	public interface IDomainResultBase
	{
		/// <summary>
		///		Collection of error messages if any
		/// </summary>
		IReadOnlyCollection<string> Errors { get; }
		/// <summary>
		///		Flag, whether the current status is successful or not
		/// </summary>
		bool IsSuccess { get; }
		/// <summary>
		///		Current status of the domain operation
		/// </summary>
		DomainOperationStatus Status { get; }
	}
}
