namespace DomainResults.Common;

/// <summary>
///		Base interface for <see cref="IDomainResult"/> and <see cref="IDomainResult{T}"/>
/// </summary>
public interface IDomainResultBase
{
	/// <summary>
	///		Collection of error messages if any
	/// </summary>
	/// <remarks>
	///		Each message is the text of a <see cref="DomainErrors"/> item: the message, followed by the member names in brackets if any.
	/// </remarks>
	IReadOnlyCollection<string> Errors { get; }
		
	/// <summary>
	///		Error messages joined into a single line ('.'-separated messages)
	/// </summary>
	string Error => !Errors.Any() ? string.Empty : string.Join(". ", Errors);
		
	/// <summary>
	///		Collection of errors with the names of the members they are about, if any
	/// </summary>
	/// <remarks>
	///		It is the source of <see cref="Errors"/>. Only a result from validation results has member names.
	///		The default implementation returns each item of <see cref="Errors"/> without member names.
	/// </remarks>
	IReadOnlyCollection<DomainError> DomainErrors => Errors.Select(error => new DomainError(error)).ToArray();

	/// <summary>
	///		Flag, whether the current status is successful or not
	/// </summary>
	bool IsSuccess { get; }
	/// <summary>
	///		Current status of the domain operation
	/// </summary>
	DomainOperationStatus Status { get; }
}