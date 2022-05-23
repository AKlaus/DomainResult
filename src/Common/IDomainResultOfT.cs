using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace DomainResults.Common
{
	/// <summary>
	///		Represents the status id the domain operation along with returned value
	/// </summary>
	/// <typeparam name="T"> Value type of returned by the domain operation </typeparam>
	public interface IDomainResult<T> : IDomainResultBase
	{
		/// <summary>
		///		Value returned by the domain operation
		/// </summary>
		[AllowNull]
		T Value { get; }

		/// <summary>
		///		Gets the <see cref="Value"/> if <see cref="IDomainResultBase.Status"/> is <see cref="DomainOperationStatus.Success"/>
		/// </summary>
		/// <param name="value">
		///		When this method returns, contains <see cref="Value"/> if <see cref="IDomainResultBase.IsSuccess"/> is <see langword="true" />; otherwise, the default value for the type <typeparamref name="T"/>. This parameter is passed uninitialized.
		/// </param>
		/// <returns> <see langword="true" /> if the value is returned; otherwise, <see langword="false" /> </returns>
		bool TryGetValue([MaybeNullWhen(false)] out T value);

		/// <summary>
		/// 	Deconstructs the instance to a (TValue, IDomainResult) pair
		/// </summary>
		/// <param name="value"> The result value returned by the domain operation </param>
		/// <param name="details"> Details of the domain operation (like status) </param>
		void Deconstruct([AllowNull] out T value, out IDomainResult details) => (value, details) = (Value, new DomainResult(Status, Errors));

		// TODO: Make a call on using either the extension methods in this interface (below) (and move them to 'IDomainResult') or extension methods on DomainResult class 

		#region Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------------

		/// <summary>
		///		Get 'success' status with a value. Gets converted to HTTP code 200 (Ok)
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		static IDomainResult<T> Success(T value)						=> DomainResult<T>.Success(value);

		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static IDomainResult<T> NotFound(string? message = null)		=> DomainResult<T>.NotFound(message);
		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static IDomainResult<T> NotFound(IEnumerable<string> messages)	=> DomainResult<T>.NotFound(messages);

		/// <summary>
		///		Get 'Unauthorized' status. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static IDomainResult<T> Unauthorized(string? message = null)	=> DomainResult<T>.Unauthorized(message);

		/// <summary>
		///		Get 'failed' status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static IDomainResult<T> Failed(string? error = null)			=> DomainResult<T>.Failed(error);
		/// <summary>
		///		Get 'failed' status. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static IDomainResult<T> Failed(IEnumerable<string> errors)		=> DomainResult<T>.Failed(errors);
		/// <summary>
		///		Get 'failed' status with validation errors. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static IDomainResult<T> Failed(IEnumerable<ValidationResult> validationResults) => DomainResult<T>.Failed(validationResults);

		/// <summary>
		///		Get 'Critical error' for a dependency status. Gets converted to HTTP code 503 (Service Unavailable)
		/// </summary>
		/// <param name="error"> Optional error message </param>
		static IDomainResult<T> CriticalDependencyError(string? error = null)	=> DomainResult<T>.CriticalDependencyError(error);

		#endregion // Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------

		/// <summary>
		/// 	Convert to a <see cref="IDomainResult{T}" /> with a new value type <typeparamref name="TNew"/>
		/// </summary>
		/// <typeparam name="TNew"> The new value type (converting to) </typeparam>
		IDomainResult<TNew> To<TNew>() => DomainResult<TNew>.From(this);

		#region Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -------

		/// <summary>
		///		Get 'success' status with a value (all wrapped in <see cref="Task{T}"/>).
		///		Gets converted to HTTP code 200 (Ok)
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		static Task<IDomainResult<T>> SuccessTask(T value)						=> DomainResult<T>.SuccessTask(value);

		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static Task<IDomainResult<T>> NotFoundTask(string? message = null)		=> DomainResult<T>.NotFoundTask(message);
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		static Task<IDomainResult<T>> NotFoundTask(IEnumerable<string> messages)=> DomainResult<T>.NotFoundTask(messages);

		/// <summary>
		///		Get 'unauthorized' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <param name="message"> Optional message </param>
		static Task<IDomainResult<T>> UnauthorizedTask(string? message = null)	=> DomainResult<T>.UnauthorizedTask(message);

		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		static Task<IDomainResult<T>> FailedTask(string? error = null)			=> DomainResult<T>.FailedTask(error);
		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		static Task<IDomainResult<T>> FailedTask(IEnumerable<string> errors)	=> DomainResult<T>.FailedTask(errors);
		/// <summary>
		///		Get 'failed' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		static Task<IDomainResult<T>> FailedTask(IEnumerable<ValidationResult> validationResults) => DomainResult<T>.FailedTask(validationResults);

		/// <summary>
		///		Get 'Critical error' for a dependency status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 503 (Service Unavailable)
		/// </summary>
		/// <param name="error"> Optional error message </param>
		static Task<IDomainResult<T>> CriticalDependencyErrorTask(string? error = null)	=> DomainResult<T>.CriticalDependencyErrorTask(error);

		#endregion // Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -
	}
}
