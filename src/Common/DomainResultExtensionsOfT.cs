using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DomainResults.Common
{
	public partial class DomainResult
	{
		// Aliases for DomainResult<TValue> extensions [PUBLIC, STATIC]

		#region Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------------

		/// <summary>
		///		Get 'success' status with a value. Gets converted to HTTP code 200 (Ok)
		/// </summary>
		/// <typeparam name="TValue"> The value type </typeparam>
		/// <param name="value"> The value to be returned </param>
		public static IDomainResult<TValue> Success<TValue>(TValue value)					=> DomainResult<TValue>.Success(value);

		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> NotFound<TValue>(string? message = null)		=> DomainResult<TValue>.NotFound(message);
		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="messages"> Custom messages </param>
		public static IDomainResult<TValue> NotFound<TValue>(IEnumerable<string> messages)	=> DomainResult<TValue>.NotFound(messages);

		/// <summary>
		///		Get 'Unauthorized' status. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> Unauthorized<TValue>(string? message = null)	=> DomainResult<TValue>.Unauthorized(message);

		/// <summary>
		///		Get 'failed' status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static IDomainResult<TValue> Failed<TValue>(string? error = null)			=> DomainResult<TValue>.Failed(error);
		/// <summary>
		///		Get 'failed' status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="errors"> Custom messages </param>
		public static IDomainResult<TValue> Failed<TValue>(IEnumerable<string> errors)		=> DomainResult<TValue>.Failed(errors);
		/// <summary>
		///		Get 'failed' status with validation errors. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="validationResults"> Results of a validation request </param>
		public static IDomainResult<TValue> Failed<TValue>(IEnumerable<ValidationResult> validationResults) 
																							=> DomainResult<TValue>.Failed(validationResults);

		/// <summary>
		///		Get 'Critical error' for a dependency status. Gets converted to HTTP code 503 (Service Unavailable)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static IDomainResult<TValue> CriticalDependencyError<TValue>(string? error = null)		
																							=> DomainResult<TValue>.CriticalDependencyError(error);

		#endregion // Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------

		#region Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -------

		/// <summary>
		///		Get 'success' status with a value (all wrapped in <see cref="Task{T}"/>).
		///		Gets converted to HTTP code 200 (Ok)
		/// </summary>
		/// <typeparam name="TValue"> The value type </typeparam>
		/// <param name="value"> The value to be returned </param>
		public static Task<IDomainResult<TValue>> SuccessTask<TValue>(TValue value)					=> DomainResult<TValue>.SuccessTask(value);

		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(string? message = null)		=> DomainResult<TValue>.NotFoundTask(message);
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="messages"> Custom messages </param>
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(IEnumerable<string> messages)=> DomainResult<TValue>.NotFoundTask(messages);

		/// <summary>
		///		Get 'Unauthorized' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> UnauthorizedTask<TValue>(string? message = null)	=> DomainResult<TValue>.UnauthorizedTask(message);
		
		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(string? error = null)			=> DomainResult<TValue>.FailedTask(error);
		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="errors"> Custom messages </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(IEnumerable<string> errors)	=> DomainResult<TValue>.FailedTask(errors);
		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="validationResults"> Results of a validation request </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(IEnumerable<ValidationResult> validationResults) 
																									=> DomainResult<TValue>.FailedTask(validationResults);

		/// <summary>
		///		Get 'Critical error' for a dependency status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 503 (Service Unavailable)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static Task<IDomainResult<TValue>> CriticalDependencyErrorTask<TValue>(string? error = null)	
																									=> DomainResult<TValue>.CriticalDependencyErrorTask(error);

		#endregion // Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -
	}
}
