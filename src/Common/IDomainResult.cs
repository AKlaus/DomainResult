using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DomainResults.Common
{
	/// <summary>
	///		Defines a status of the domain operation (e.g. 'success', 'not found', etc.)
	/// </summary>
	public interface IDomainResult: IDomainResultBase
	{
#if !NETSTANDARD2_0 && !NETCOREAPP2_0 && !NETCOREAPP2_1

		#region Extensions of 'IDomainResult' [STATIC] ------------------------

		/// <summary>
		///		Get 'success' status. Gets converted to HTTP code 204 (NoContent)
		/// </summary>
		static IDomainResult Success()								=> DomainResult.Success();
		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static IDomainResult NotFound(string? message = null)		=> DomainResult.NotFound(message);
		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static IDomainResult NotFound(IEnumerable<string> messages) => DomainResult.NotFound(messages);
		/// <summary>
		///		Get 'unauthorized' status. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static IDomainResult Unauthorized(string? message = null)	=> DomainResult.Unauthorized(message);
		/// <summary>
		///		Get 'error' status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static IDomainResult Failed(string? error = null)			=> DomainResult.Failed(error);
		/// <summary>
		///		Get 'error' status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static IDomainResult Failed(IEnumerable<string> errors)		=> DomainResult.Failed(errors);
		/// <summary>
		///		Get 'error' status with validation errors. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static IDomainResult Failed(IEnumerable<ValidationResult> validationResults) => DomainResult.Failed(validationResults);

		/// <summary>
		///		Get 'success' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 204 (NoContent)
		/// </summary>
		static Task<IDomainResult> SuccessTask()								=> DomainResult.SuccessTask();
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static Task<IDomainResult> NotFoundTask(string? message = null)			=> DomainResult.NotFoundTask(message);
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static Task<IDomainResult> NotFoundTask(IEnumerable<string> messages)	=> DomainResult.NotFoundTask(messages);
		/// <summary>
		///		Get 'unauthorized' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static Task<IDomainResult> UnauthorizedTask(string? message = null)		=> DomainResult.UnauthorizedTask(message);
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static Task<IDomainResult> FailedTask(string? error = null)				=> DomainResult.FailedTask(error);
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static Task<IDomainResult> FailedTask(IEnumerable<string> errors)		=> DomainResult.FailedTask(errors);
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static Task<IDomainResult> FailedTask(IEnumerable<ValidationResult> validationResults) => DomainResult.FailedTask(validationResults);

		#endregion // Extensions of 'IDomainResult' [STATIC] ------------------

		#region Extensions of '(TValue, IDomainResult)' [STATIC] --------------

		/// <summary>
		///		Get 'success' status. Gets converted to HTTP code 200 (Ok)
		/// </summary>
		static (TValue, IDomainResult) Success<TValue>(TValue value)				 => (value, DomainResult.Success());
		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static (TValue, IDomainResult) NotFound<TValue>(string? message = null)		 => (default, DomainResult.NotFound(message));
		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static (TValue, IDomainResult) NotFound<TValue>(IEnumerable<string> messages)=> (default, DomainResult.NotFound(messages));
		/// <summary>
		///		Get 'unauthorized' status. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static (TValue, IDomainResult) Unauthorized<TValue>(string? message = null)	 => (default, DomainResult.Unauthorized(message));
		/// <summary>
		///		Get 'failed' status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static (TValue, IDomainResult) Failed<TValue>(string? error = null)			 => (default, DomainResult.Failed(error));
		/// <summary>
		///		Get 'failed' status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static (TValue, IDomainResult) Failed<TValue>(IEnumerable<string> errors)	 => (default, DomainResult.Failed(errors));
		/// <summary>
		///		Get 'failed' status with validation errors. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static (TValue, IDomainResult) Failed<TValue>(IEnumerable<ValidationResult> validationResults) 
																					 => (default, DomainResult.Failed(validationResults));

		/// <summary>
		///		Get 'success' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 200 (Ok)
		/// </summary>
		static Task<(TValue, IDomainResult)> SuccessTask<TValue>(TValue value)				   => Task.FromResult(Success(value));
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(string? message = null)	   => Task.FromResult(NotFound<TValue>(message));
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(IEnumerable<string> messages)=> Task.FromResult(NotFound<TValue>(messages));
		/// <summary>
		///		Get 'unauthorized' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static Task<(TValue, IDomainResult)> UnauthorizedTask<TValue>(string? message = null)	=> Task.FromResult(Unauthorized<TValue>(message));
		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static Task<(TValue, IDomainResult)> FailedTask<TValue>(string? error = null)		   => Task.FromResult(Failed<TValue>(error));
		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static Task<(TValue, IDomainResult)> FailedTask<TValue>(IEnumerable<string> errors)	   => Task.FromResult(Failed<TValue>(errors));
		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static Task<(TValue, IDomainResult)> FailedTask<TValue>(IEnumerable<ValidationResult> validationResults) 
																								=> Task.FromResult(Failed<TValue>(validationResults));

		#endregion // Extensions of '(TValue, IDomainResult)' [STATIC] --------
#endif
	}
}