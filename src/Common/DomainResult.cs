using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

// ReSharper disable MemberCanBePrivate.Global

namespace DomainResults.Common;

/// <inheritdoc/>
public partial class DomainResult : IDomainResult
{
	/// <inheritdoc/>
	public DomainOperationStatus Status { get; }
	/// <inheritdoc/>
	public IReadOnlyCollection<string> Errors { get; }
	/// <inheritdoc/>
	public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
	/// <inheritdoc/>
	public bool IsSuccess => Status == DomainOperationStatus.Success;

	internal static readonly IReadOnlyDictionary<string, string[]> NoValidationErrors = new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>());

	#region Constructors [PUBLIC, PROTECTED] ------------------------------

	/// <summary>
	///		Creates a new instance with <see cref="DomainOperationStatus.Success"/> status
	/// </summary>
	protected DomainResult()											: this(DomainOperationStatus.Success, (string?)null) { }
	/// <summary>
	///		Creates a new instance with a specified status and error messages
	/// </summary>
	/// <param name="status"> The state </param>
	/// <param name="error"> Optional custom error messages </param>
	protected DomainResult(DomainOperationStatus status, string? error)	: this(status, !string.IsNullOrEmpty(error) ? new[] { error } : Array.Empty<string>()) { }
	/// <summary>
	///		Creates a new instance with a faulty status and error messages
	/// </summary>
	/// <param name="status"> The state </param>
	/// <param name="errors"> Custom error messages </param>
	public DomainResult(DomainOperationStatus status, IEnumerable<string> errors) : this(status, errors, NoValidationErrors) { }
	/// <summary>
	///		Creates a new instance with a status, error messages and validation error messages keyed by member name
	/// </summary>
	internal DomainResult(DomainOperationStatus status, IEnumerable<string> errors, IReadOnlyDictionary<string, string[]> validationErrors)
	{
		Status = status;
		Errors = errors.ToArray();
		ValidationErrors = validationErrors;
	}
	/// <summary>
	///		Creates a new instance with faulty status and validation error messages
	/// </summary>
	/// <param name="validationResults"> Validation error messages </param>
	protected DomainResult(IEnumerable<ValidationResult> validationResults)
	{
		var results = validationResults.ToArray();
		Status = DomainOperationStatus.Failed;
		Errors = (from message in results
				select $"{message.ErrorMessage}{(message.MemberNames?.Any() == true ? " (" + string.Join(", ", message.MemberNames) + ")" : "")}"
			).ToArray();
		ValidationErrors = ToValidationErrors(results);
	}
	#endregion // Constructors [PUBLIC, PROTECTED] ------------------------

	#region Extensions of 'IDomainResult' [STATIC, PUBLIC] ----------------

	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Success"/>. Gets converted to HTTP code 204 (NoContent)
	/// </summary>
	public static IDomainResult Success()							 	=> new DomainResult();
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/>. Gets converted to HTTP code 404 (NotFound) at the API level
	/// </summary>
	/// <param name="message"> Optional message </param>
	public static IDomainResult NotFound(string? message = null)		=> new DomainResult(DomainOperationStatus.NotFound, message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="messages"> Custom messages </param>
	public static IDomainResult NotFound(IEnumerable<string> messages)	=> new DomainResult(DomainOperationStatus.NotFound, messages);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Unauthorized"/>. Gets converted to HTTP code 403 (Forbidden) at the API level
	/// </summary>
	/// <param name="message"> Optional message </param>
	public static IDomainResult Unauthorized(string? message = null)	=> new DomainResult(DomainOperationStatus.Unauthorized, message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Conflict"/>. Gets converted to HTTP code 409 (Conflict) at the API level
	/// </summary>
	/// <param name="message"> Optional message </param>
	public static IDomainResult Conflict(string? message = null)		=> new DomainResult(DomainOperationStatus.Conflict, message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.ContentTooLarge"/>. Gets converted to HTTP code 413 (ContentTooLarge) at the API level
	/// </summary>
	/// <param name="message"> Optional message </param>
	public static IDomainResult ContentTooLarge(string? message = null)	=> new DomainResult(DomainOperationStatus.ContentTooLarge, message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="error"> Optional message </param>
	public static IDomainResult Failed(string? error = null)			=> new DomainResult(DomainOperationStatus.Failed, error);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="errors"> Custom messages </param>
	public static IDomainResult Failed(IEnumerable<string> errors)		=> new DomainResult(DomainOperationStatus.Failed, errors);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> with validation errors. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="validationResults"> Results of a validation request </param>
	public static IDomainResult Failed(IEnumerable<ValidationResult> validationResults) => new DomainResult(validationResults);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.CriticalDependencyError"/> status (failed dependency call). Gets converted to HTTP code 503 (Service Unavailable)
	/// </summary>
	/// <param name="error"> Optional error message </param>
	public static IDomainResult CriticalDependencyError(string? error = null)	=> new DomainResult(DomainOperationStatus.CriticalDependencyError, error);

	#endregion // Extensions of 'IDomainResult' [STATIC, PUBLIC] ----------

	#region Extensions of 'Task<IDomainResult>' [STATIC, PUBLIC] ----------

	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Success"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 204 (NoContent)
	/// </summary>
	public static Task<IDomainResult> SuccessTask()								=> Task.FromResult(Success());
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="message"> Optional message </param>
	public static Task<IDomainResult> NotFoundTask(string? message = null)		=> Task.FromResult(NotFound(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="messages"> Custom messages </param>
	public static Task<IDomainResult> NotFoundTask(IEnumerable<string> messages)=> Task.FromResult(NotFound(messages));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Unauthorized"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
	/// </summary>
	/// <param name="message"> Optional message </param>
	public static Task<IDomainResult> UnauthorizedTask(string? message = null)	=> Task.FromResult(Unauthorized(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Conflict"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 409 (Conflict)
	/// </summary>
	/// <param name="message"> Optional message </param>
	public static Task<IDomainResult> ConflictTask(string? message = null)		=> Task.FromResult(Conflict(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.ContentTooLarge"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 413 (ContentTooLarge)
	/// </summary>
	/// <param name="message"> Optional message </param>
	public static Task<IDomainResult> ContentTooLargeTask(string? message = null)		=> Task.FromResult(ContentTooLarge(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="error"> Optional message </param>
	public static Task<IDomainResult> FailedTask(string? error = null)			=> Task.FromResult(Failed(error));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="errors"> Custom messages </param>
	public static Task<IDomainResult> FailedTask(IEnumerable<string> errors)	=> Task.FromResult(Failed(errors));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="validationResults"> Results of a validation request </param>
	public static Task<IDomainResult> FailedTask(IEnumerable<ValidationResult> validationResults) => Task.FromResult(Failed(validationResults));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.CriticalDependencyError"/> status (failed dependency call) wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 503 (Service Unavailable)
	/// </summary>
	/// <param name="error"> Optional message </param>
	public static Task<IDomainResult> CriticalDependencyErrorTask(string? error = null)	=> Task.FromResult(CriticalDependencyError(error));

	#endregion // Extensions of 'Task<IDomainResult>' [STATIC, PUBLIC] ----

	/// <summary>
	///		Groups the validation error messages by member name. A message without a member name gets the key <see cref="string.Empty"/>
	/// </summary>
	private static IReadOnlyDictionary<string, string[]> ToValidationErrors(IEnumerable<ValidationResult> validationResults)
		=> new ReadOnlyDictionary<string, string[]>(
			(from result in validationResults
			 from member in result.MemberNames?.Any() == true ? result.MemberNames : new[] { string.Empty }
			 group result.ErrorMessage ?? string.Empty by member ?? string.Empty
			 ).ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal));
}