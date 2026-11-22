namespace DomainResults.Common;

/// <summary>
///		An error message of a domain operation, with the names of the members it is about
/// </summary>
public sealed class DomainError
{
	/// <summary>
	///		Creates a new instance with an error message and optional member names
	/// </summary>
	/// <param name="message"> The error message </param>
	/// <param name="memberNames"> The names of the members that the message is about </param>
	internal DomainError(string message, IEnumerable<string?>? memberNames = null)
	{
		Message = message;
		MemberNames = memberNames?.Select(name => name ?? string.Empty).ToArray() ?? Array.Empty<string>();
	}

	/// <summary>
	///		The error message
	/// </summary>
	public string Message { get; }

	/// <summary>
	///		The names of the members that the message is about, if any
	/// </summary>
	public IReadOnlyCollection<string> MemberNames { get; }

	/// <summary>
	///		Returns the message, followed by the member names in brackets if any
	/// </summary>
	/// <remarks>
	///		<see cref="IDomainResultBase.Errors"/> holds this text for each <see cref="DomainError"/>.
	/// </remarks>
	public override string ToString() => MemberNames.Count > 0 ? $"{Message} ({string.Join(", ", MemberNames)})" : Message;
}
