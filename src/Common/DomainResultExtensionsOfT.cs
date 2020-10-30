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
		///		Get 'success' status with a value. Later it can be converted to HTTP code 200 (Ok)
		/// </summary>
		/// <typeparam name="TValue"> The value type </typeparam>
		/// <param name="value"> The value to be returned </param>
		public static IDomainResult<TValue> Success<TValue>(TValue value)					=> DomainResult<TValue>.Success(value);

		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> NotFound<TValue>(string? message = null)		=> DomainResult<TValue>.NotFound(message);
		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="messages"> Custom messages </param>
		public static IDomainResult<TValue> NotFound<TValue>(IEnumerable<string> messages)	=> DomainResult<TValue>.NotFound(messages);

		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static IDomainResult<TValue> Failed<TValue>(string? error = null)				=> DomainResult<TValue>.Error(error);
		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="errors"> Custom messages </param>
		public static IDomainResult<TValue> Failed<TValue>(IEnumerable<string> errors)		=> DomainResult<TValue>.Error(errors);
		/// <summary>
		///		Get 'error' status with validation errors. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="validationResults"> Results of a validation request </param>
		public static IDomainResult<TValue> Failed<TValue>(IEnumerable<ValidationResult> validationResults) => DomainResult<TValue>.Error(validationResults);

		#endregion // Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------

		#region Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -------

		/// <summary>
		///		Get 'success' status with a value (all wrapped in <see cref="Task{T}"/>).
		///		Later it can be converted to HTTP code 200 (Ok)
		/// </summary>
		/// <typeparam name="TValue"> The value type </typeparam>
		/// <param name="value"> The value to be returned </param>
		public static Task<IDomainResult<TValue>> SuccessTask<TValue>(TValue value)					=> DomainResult<TValue>.SuccessTask(value);

		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(string? message = null)		=> DomainResult<TValue>.NotFoundTask(message);
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="messages"> Custom messages </param>
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(IEnumerable<string> messages)=> DomainResult<TValue>.NotFoundTask(messages);

		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="error"> Optional message </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(string? error = null)			=> DomainResult<TValue>.ErrorTask(error);
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="errors"> Custom messages </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(IEnumerable<string> errors)		=> DomainResult<TValue>.ErrorTask(errors);
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <typeparam name="TValue"> The expected value type if the operation was successful </typeparam>
		/// <param name="validationResults"> Results of a validation request </param>
		public static Task<IDomainResult<TValue>> FailedTask<TValue>(IEnumerable<ValidationResult> validationResults) => DomainResult<TValue>.ErrorTask(validationResults);

		#endregion // Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -
	}
}
