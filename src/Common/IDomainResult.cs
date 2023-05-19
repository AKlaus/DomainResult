using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DomainResults.Common;

/// <summary>
///		Defines a status of the domain operation (e.g. 'success', 'not found', etc.)
/// </summary>
public interface IDomainResult: IDomainResultBase
{
	#region Extensions of 'IDomainResult' [STATIC] ------------------------

	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Success"/>. Gets converted to HTTP code 204 (NoContent)
	/// </summary>
	static IDomainResult Success()								=> DomainResult.Success();
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static IDomainResult NotFound(string? message = null)		=> DomainResult.NotFound(message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="messages"> Custom messages </param>
	static IDomainResult NotFound(IEnumerable<string> messages) => DomainResult.NotFound(messages);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Unauthorized"/>. Gets converted to HTTP code 403 (Forbidden)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static IDomainResult Unauthorized(string? message = null)	=> DomainResult.Unauthorized(message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Conflict"/>. Gets converted to HTTP code 409 (Conflict)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static IDomainResult Conflict(string? message = null)	=> DomainResult.Conflict(message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.PayloadTooLarge"/>. Gets converted to HTTP code 413 (PayloadTooLarge)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static IDomainResult PayloadTooLarge(string? message = null)	=> DomainResult.PayloadTooLarge(message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="error"> Optional message </param>
	static IDomainResult Failed(string? error = null)			=> DomainResult.Failed(error);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="errors"> Custom messages </param>
	static IDomainResult Failed(IEnumerable<string> errors)		=> DomainResult.Failed(errors);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status with validation errors. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="validationResults"> Results of a validation request </param>
	static IDomainResult Failed(IEnumerable<ValidationResult> validationResults) => DomainResult.Failed(validationResults);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.CriticalDependencyError"/> status (failed dependency call). Gets converted to HTTP code 503 (Service Unavailable)
	/// </summary>
	/// <param name="error"> Optional error message </param>
	static IDomainResult CriticalDependencyError(string? error = null)	=> DomainResult.CriticalDependencyError(error);

	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Success"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 204 (NoContent)
	/// </summary>
	static Task<IDomainResult> SuccessTask()								=> DomainResult.SuccessTask();
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static Task<IDomainResult> NotFoundTask(string? message = null)			=> DomainResult.NotFoundTask(message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="messages"> Custom messages </param>
	static Task<IDomainResult> NotFoundTask(IEnumerable<string> messages)	=> DomainResult.NotFoundTask(messages);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Unauthorized"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static Task<IDomainResult> UnauthorizedTask(string? message = null)		=> DomainResult.UnauthorizedTask(message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Conflict"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 409 (Conflict)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static Task<IDomainResult> ConflictTask(string? message = null)		=> DomainResult.ConflictTask(message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.PayloadTooLarge"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 413 (PayloadTooLarge)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static Task<IDomainResult> PayloadTooLargeTask(string? message = null)		=> DomainResult.PayloadTooLargeTask(message);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="error"> Optional message </param>
	static Task<IDomainResult> FailedTask(string? error = null)				=> DomainResult.FailedTask(error);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="errors"> Custom messages </param>
	static Task<IDomainResult> FailedTask(IEnumerable<string> errors)		=> DomainResult.FailedTask(errors);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="validationResults"> Results of a validation request </param>
	static Task<IDomainResult> FailedTask(IEnumerable<ValidationResult> validationResults)
																			=> DomainResult.FailedTask(validationResults);
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.CriticalDependencyError"/> status (failed dependency call) wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 503 (Service Unavailable)
	/// </summary>
	/// <param name="error"> Optional error message </param>
	static Task<IDomainResult> CriticalDependencyErrorTask(string? error = null)
																			=> DomainResult.CriticalDependencyErrorTask(error);

	#endregion // Extensions of 'IDomainResult' [STATIC] ------------------

	#region Extensions of '(TValue, IDomainResult)' [STATIC] --------------
#pragma warning disable CS8619

	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Success"/>. Gets converted to HTTP code 200 (Ok)
	/// </summary>
	static (TValue, IDomainResult) Success<TValue>(TValue value)				 => (value, DomainResult.Success());
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static (TValue, IDomainResult) NotFound<TValue>(string? message = null)		 => (default, DomainResult.NotFound(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="messages"> Custom messages </param>
	static (TValue, IDomainResult) NotFound<TValue>(IEnumerable<string> messages)=> (default, DomainResult.NotFound(messages));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Unauthorized"/>. Gets converted to HTTP code 403 (Forbidden)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static (TValue, IDomainResult) Unauthorized<TValue>(string? message = null)	 => (default, DomainResult.Unauthorized(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Conflict"/>. Gets converted to HTTP code 409 (Conflict)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static (TValue, IDomainResult) Conflict<TValue>(string? message = null)	 => (default, DomainResult.Conflict(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.PayloadTooLarge"/>. Gets converted to HTTP code 413 (PayloadTooLarge)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static (TValue, IDomainResult) PayloadTooLarge<TValue>(string? message = null)	 => (default, DomainResult.PayloadTooLarge(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="error"> Optional message </param>
	static (TValue, IDomainResult) Failed<TValue>(string? error = null)			 => (default, DomainResult.Failed(error));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="errors"> Custom messages </param>
	static (TValue, IDomainResult) Failed<TValue>(IEnumerable<string> errors)	 => (default, DomainResult.Failed(errors));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status with validation errors. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="validationResults"> Results of a validation request </param>
	static (TValue, IDomainResult) Failed<TValue>(IEnumerable<ValidationResult> validationResults) 
																				 => (default, DomainResult.Failed(validationResults));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.CriticalDependencyError"/> status (failed dependency call). Gets converted to HTTP code 503 (Service Unavailable)
	/// </summary>
	/// <param name="error"> Optional error message </param>
	static (TValue, IDomainResult) CriticalDependencyError<TValue>(string? error = null)
																				=> (default, DomainResult.CriticalDependencyError(error));

	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Success"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 200 (Ok)
	/// </summary>
	static Task<(TValue, IDomainResult)> SuccessTask<TValue>(TValue value)				   => Task.FromResult(Success(value));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(string? message = null)	   => Task.FromResult(NotFound<TValue>(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.NotFound"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
	/// </summary>
	/// <param name="messages"> Custom messages </param>
	static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(IEnumerable<string> messages)=> Task.FromResult(NotFound<TValue>(messages));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Unauthorized"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static Task<(TValue, IDomainResult)> UnauthorizedTask<TValue>(string? message = null)	=> Task.FromResult(Unauthorized<TValue>(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Conflict"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 409 (Conflict)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static Task<(TValue, IDomainResult)> ConflictTask<TValue>(string? message = null)	=> Task.FromResult(Conflict<TValue>(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.PayloadTooLarge"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 413 (PayloadTooLarge)
	/// </summary>
	/// <param name="message"> Optional message </param>
	static Task<(TValue, IDomainResult)> PayloadTooLargeTask<TValue>(string? message = null)	=> Task.FromResult(PayloadTooLarge<TValue>(message));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="error"> Optional message </param>
	static Task<(TValue, IDomainResult)> FailedTask<TValue>(string? error = null)		   => Task.FromResult(Failed<TValue>(error));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="errors"> Custom messages </param>
	static Task<(TValue, IDomainResult)> FailedTask<TValue>(IEnumerable<string> errors)	   => Task.FromResult(Failed<TValue>(errors));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
	/// </summary>
	/// <param name="validationResults"> Results of a validation request </param>
	static Task<(TValue, IDomainResult)> FailedTask<TValue>(IEnumerable<ValidationResult> validationResults) 
																							=> Task.FromResult(Failed<TValue>(validationResults));
	/// <summary>
	///		Returns <see cref="DomainOperationStatus.CriticalDependencyError"/> status (failed dependency call) wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 503 (Service Unavailable)
	/// </summary>
	/// <param name="error"> Optional error message </param>
	static Task<(TValue, IDomainResult)> CriticalDependencyErrorTask<TValue>(string? error = null)
																							=> Task.FromResult(CriticalDependencyError<TValue>(error));

#pragma warning restore CS8619
	#endregion // Extensions of '(TValue, IDomainResult)' [STATIC] --------
}