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
		///		Returns <see cref="DomainOperationStatus.Success"/> status with a value. Gets converted to HTTP code 200 (Ok)
		/// </summary>
		/// <typeparam name="TValue"> The value type </typeparam>
		/// <param name="value"> The value to be returned </param>
		public static IDomainResult<TValue> Success<TValue>(TValue value)					=> DomainResult<TValue>.Success(value);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.NotFound"/> status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> NotFound<TValue>(string? message = null)		=> DomainResult<TValue>.NotFound(message);
		/// <summary>
		///		Returns <see cref="DomainOperationStatus.NotFound"/> status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="messages"> Custom messages </param>
		public static IDomainResult<TValue> NotFound<TValue>(IEnumerable<string> messages)	=> DomainResult<TValue>.NotFound(messages);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Unauthorized"/> status. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> Unauthorized<TValue>(string? message = null)	=> DomainResult<TValue>.Unauthorized(message);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Conflict"/> status. Gets converted to HTTP code 409 (Conflict)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> Conflict<TValue>(string? message = null)		=> DomainResult<TValue>.Conflict(message);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Failed"/> status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static IDomainResult<TValue> Failed<TValue>(string? error = null)			=> DomainResult<TValue>.Failed(error);
		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Failed"/> status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="errors"> Custom messages </param>
		public static IDomainResult<TValue> Failed<TValue>(IEnumerable<string> errors)		=> DomainResult<TValue>.Failed(errors);
		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Failed"/> status with validation errors. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="validationResults"> Results of a validation request </param>
		public static IDomainResult<TValue> Failed<TValue>(IEnumerable<ValidationResult> validationResults) 
																							=> DomainResult<TValue>.Failed(validationResults);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.CriticalDependencyError"/> (failed dependency call) status. Gets converted to HTTP code 503 (Service Unavailable)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static IDomainResult<TValue> CriticalDependencyError<TValue>(string? error = null)		
																							=> DomainResult<TValue>.CriticalDependencyError(error);

		#endregion // Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------

		#region Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -------

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Success"/> status with a value (all wrapped in <see cref="Task{T}"/>).
		///		Gets converted to HTTP code 200 (Ok)
		/// </summary>
		/// <typeparam name="TValue"> The value type </typeparam>
		/// <param name="value"> The value to be returned </param>
		public static Task<IDomainResult<TValue>> SuccessTask<TValue>(TValue value)					=> DomainResult<TValue>.SuccessTask(value);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.NotFound"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(string? message = null)		=> DomainResult<TValue>.NotFoundTask(message);
		/// <summary>
		///		Returns <see cref="DomainOperationStatus.NotFound"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="messages"> Custom messages </param>
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(IEnumerable<string> messages)=> DomainResult<TValue>.NotFoundTask(messages);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Unauthorized"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> UnauthorizedTask<TValue>(string? message = null)	=> DomainResult<TValue>.UnauthorizedTask(message);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Conflict"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 409 (Conflict)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> ConflictTask<TValue>(string? message = null)	=> DomainResult<TValue>.ConflictTask(message);
		
		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(string? error = null)			=> DomainResult<TValue>.FailedTask(error);
		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="errors"> Custom messages </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(IEnumerable<string> errors)	=> DomainResult<TValue>.FailedTask(errors);
		/// <summary>
		///		Returns <see cref="DomainOperationStatus.Failed"/> status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="validationResults"> Results of a validation request </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(IEnumerable<ValidationResult> validationResults) 
																									=> DomainResult<TValue>.FailedTask(validationResults);

		/// <summary>
		///		Returns <see cref="DomainOperationStatus.CriticalDependencyError"/> status (failed dependency call) wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 503 (Service Unavailable)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static Task<IDomainResult<TValue>> CriticalDependencyErrorTask<TValue>(string? error = null)	
																									=> DomainResult<TValue>.CriticalDependencyErrorTask(error);

		#endregion // Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -
	}
}
