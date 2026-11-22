namespace DomainResults.Common;

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
	///		Error messages joined into a single line ('.'-separated messages)
	/// </summary>
	string Error => !Errors.Any() ? string.Empty : string.Join(". ", Errors);
		
	/// <summary>
	///		Validation error messages keyed by member name, if the result came from validation results
	/// </summary>
	/// <remarks>
	///		A message without a member name has the key <see cref="string.Empty"/>. A message with several member names appears under each of them.
	///		<see cref="Errors"/> holds the same messages, with the member names in brackets.
	/// </remarks>
	IReadOnlyDictionary<string, string[]> ValidationErrors => DomainResult.NoValidationErrors;

	/// <summary>
	///		Flag, whether the current status is successful or not
	/// </summary>
	bool IsSuccess { get; }
	/// <summary>
	///		Current status of the domain operation
	/// </summary>
	DomainOperationStatus Status { get; }
}