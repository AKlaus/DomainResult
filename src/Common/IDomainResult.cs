using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DomainResults.Common
{
	/// <summary>
	///		Defines a status of the domain operation (e.g. 'success', 'error', 'not found')
	/// </summary>
	public interface IDomainResult: IDomainResultBase
	{
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1

		#region Extensions of 'IDomainResult' [STATIC] ------------------------

		/// <summary>
		///		Get 'success' status. Later it can be converted to HTTP code 204 (NoContent)
		/// </summary>
		static IDomainResult Success()								=> DomainResult.Success();
		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static IDomainResult NotFound(string? message = null)		=> DomainResult.NotFound(message);
		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static IDomainResult NotFound(IEnumerable<string> messages) => DomainResult.NotFound(messages);
		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static IDomainResult Error(string? error = null)			=> DomainResult.Error(error);
		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static IDomainResult Error(IEnumerable<string> errors)		=> DomainResult.Error(errors);
		/// <summary>
		///		Get 'error' status with validation errors. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static IDomainResult Error(IEnumerable<ValidationResult> validationResults) => DomainResult.Error(validationResults);

		/// <summary>
		///		Get 'success' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 204 (NoContent)
		/// </summary>
		static Task<IDomainResult> SuccessTask()								=> DomainResult.SuccessTask();
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static Task<IDomainResult> NotFoundTask(string? message = null)			=> DomainResult.NotFoundTask(message);
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static Task<IDomainResult> NotFoundTask(IEnumerable<string> messages)	=> DomainResult.NotFoundTask(messages);
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static Task<IDomainResult> ErrorTask(string? error = null)				=> DomainResult.ErrorTask(error);
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static Task<IDomainResult> ErrorTask(IEnumerable<string> errors)		=> DomainResult.ErrorTask(errors);
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static Task<IDomainResult> ErrorTask(IEnumerable<ValidationResult> validationResults) => DomainResult.ErrorTask(validationResults);

		#endregion // Extensions of 'IDomainResult' [STATIC] ------------------

		#region Extensions of '(TValue, IDomainResult)' [STATIC] --------------

		/// <summary>
		///		Get 'success' status. Later it can be converted to HTTP code 200 (Ok)
		/// </summary>
		static (TValue, IDomainResult) Success<TValue>(TValue value)				 => (value, DomainResult.Success());
		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static (TValue, IDomainResult) NotFound<TValue>(string? message = null)		 => (default, DomainResult.NotFound(message));
		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static (TValue, IDomainResult) NotFound<TValue>(IEnumerable<string> messages)=> (default, DomainResult.NotFound(messages));
		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static (TValue, IDomainResult) Error<TValue>(string? error = null)			 => (default, DomainResult.Error(error));
		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static (TValue, IDomainResult) Error<TValue>(IEnumerable<string> errors)	 => (default, DomainResult.Error(errors));
		/// <summary>
		///		Get 'error' status with validation errors. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static (TValue, IDomainResult) Error<TValue>(IEnumerable<ValidationResult> validationResults) 
																					 => (default, DomainResult.Error(validationResults));

		/// <summary>
		///		Get 'success' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 200 (Ok)
		/// </summary>
		static Task<(TValue, IDomainResult)> SuccessTask<TValue>(TValue value)				   => Task.FromResult(Success(value));
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(string? message = null)	   => Task.FromResult(NotFound<TValue>(message));
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(IEnumerable<string> messages)=> Task.FromResult(NotFound<TValue>(messages));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static Task<(TValue, IDomainResult)> ErrorTask<TValue>(string? error = null)		   => Task.FromResult(Error<TValue>(error));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static Task<(TValue, IDomainResult)> ErrorTask<TValue>(IEnumerable<string> errors)	   => Task.FromResult(Error<TValue>(errors));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static Task<(TValue, IDomainResult)> ErrorTask<TValue>(IEnumerable<ValidationResult> validationResults) 
																								=> Task.FromResult(Error<TValue>(validationResults));

		#endregion // Extensions of '(TValue, IDomainResult)' [STATIC] --------
#endif
	}
}