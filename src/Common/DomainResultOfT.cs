using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

// ReSharper disable MemberCanBePrivate.Global

namespace DomainResults.Common
{
	/// <inheritdoc/>
	public class DomainResult<TValue> : IDomainResult<TValue>
	{
		private readonly IDomainResult _status;

		/// <inheritdoc/>
		public DomainOperationStatus Status			=> _status.Status;
		/// <inheritdoc/>
		public IReadOnlyCollection<string> Errors	=> _status.Errors;
		/// <inheritdoc/>
		public bool IsSuccess						=> _status.IsSuccess;

		/// <inheritdoc/>
		[AllowNull] 
		public TValue Value { get; }

		#region Constructors [PROTECTED] --------------------------------------

		/// <summary>
		///		Creates a new instance with a 'error'/'not found' status
		/// </summary>
		/// <param name="errorDetails"> Error details described in <see cref="IDomainResult"/> </param>
		protected DomainResult(IDomainResult errorDetails)	: this(default, errorDetails) { }
		/// <summary>
		///		Creates a new instance with 'success' status and a value
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		protected DomainResult(TValue value)				: this(value, DomainResult.Success()) {}
		/// <summary>
		///		The most generic constructor. Creates a new instance with a specified status and error messages
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		/// <param name="errorDetails"> Error details described in <see cref="IDomainResult"/> </param>
		protected DomainResult([AllowNull] TValue value, IDomainResult errorDetails)
		{
			Value = value;
			_status = errorDetails;
		}
		
		/// <summary>
		/// 	Deconstructs the instance to a (TValue, IDomainResult) pair
		/// </summary>
		/// <param name="value"> The result value returned by the domain operation </param>
		/// <param name="details"> Details of the domain operation (like status) </param>
		public void Deconstruct(out TValue value, out IDomainResult details) => (value, details) = (Value, _status);

		#endregion // Constructors [PROTECTED] --------------------------------

		/// <inheritdoc/>
		public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
		{
			value = IsSuccess ? Value : default;
			return IsSuccess;
		}
		
		/// <summary>
		///		Implicitly converts the specified <paramref name="value"/> to an <see cref="DomainResult{TValue}"/>
		/// </summary>
		/// <param name="value"> The parameter for conversion </param>
		public static implicit operator DomainResult<TValue>(TValue value) => new DomainResult<TValue>(value);
		
		// TODO: Consider to deprecate the extension methods in this class (below) in favour of ones in 'DomainResult'

		#region Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------------

		/// <summary>
		///		Get 'success' status with a value. Gets converted to HTTP code 200 (Ok)
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		public static IDomainResult<TValue> Success(TValue value)					=> new DomainResult<TValue>(value);

		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> NotFound(string? message = null)		=> new DomainResult<TValue>(DomainResult.NotFound(message));
		/// <summary>
		///		Get 'not found' status. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		public static IDomainResult<TValue> NotFound(IEnumerable<string> messages)	=> new DomainResult<TValue>(DomainResult.NotFound(messages));

		/// <summary>
		///		Get 'Unauthorized' status. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> Unauthorized(string? message = null)	=> new DomainResult<TValue>(DomainResult.Unauthorized(message));
		
		/// <summary>
		///		Get 'failed' status. Gets be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		public static IDomainResult<TValue> Failed(string? error = null)			=> new DomainResult<TValue>(DomainResult.Failed(error));
		/// <summary>
		///		Get 'failed' status. Gets be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		public static IDomainResult<TValue> Failed(IEnumerable<string> errors)		=> new DomainResult<TValue>(DomainResult.Failed(errors));
		/// <summary>
		///		Get 'failed' status with validation errors. Gets be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		public static IDomainResult<TValue> Failed(IEnumerable<ValidationResult> validationResults) => new DomainResult<TValue>(DomainResult.Failed(validationResults));

		/// <summary>
		///		Get 'Critical error' status for a dependency. Gets converted to HTTP code 503 (Service Unavailable)
		/// </summary>
		/// <param name="error"> Optional error message </param>
		public static IDomainResult<TValue> CriticalDependencyError(string? error = null)	=> new DomainResult<TValue>(DomainResult.CriticalDependencyError(error));

		/// <summary>
		/// 	Initiate from a <see cref="IDomainResult"/> instance
		/// </summary>
		public static IDomainResult<TValue> From(IDomainResult result) => new DomainResult<TValue>(result);
		/// <summary>
		/// 	Initiate from a <see cref="IDomainResult{T}"/> instance of another 'T'
		/// </summary>
		public static IDomainResult<TValue> From<TOld>(IDomainResult<TOld> result) 
			=> new DomainResult<TValue>(default, new DomainResult(result.Status, result.Errors));
		
		#endregion // Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------

		#region Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -------

		/// <summary>
		///		Get 'success' status with a value (all wrapped in <see cref="Task{T}"/>).
		///		Gets converted to HTTP code 200 (Ok)
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		public static Task<IDomainResult<TValue>> SuccessTask(TValue value)					=> Task.FromResult(Success(value));

		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> NotFoundTask(string? message = null)		=> Task.FromResult(NotFound(message));
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		public static Task<IDomainResult<TValue>> NotFoundTask(IEnumerable<string> messages)=> Task.FromResult(NotFound(messages));

		/// <summary>
		///		Get 'Unauthorized' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 403 (Forbidden)
		/// </summary>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> UnauthorizedTask(string? message = null)	=> Task.FromResult(Unauthorized(message));

		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		public static Task<IDomainResult<TValue>> FailedTask(string? error = null)			=> Task.FromResult(Failed(error));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		public static Task<IDomainResult<TValue>> FailedTask(IEnumerable<string> errors)	=> Task.FromResult(Failed(errors));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		public static Task<IDomainResult<TValue>> FailedTask(IEnumerable<ValidationResult> validationResults)
																							=> Task.FromResult(Failed(validationResults));

		/// <summary>
		///		Get 'Critical error' for a dependency status wrapped in a <see cref="Task{T}"/>. Gets converted to HTTP code 503 (Service Unavailable)
		/// </summary>
		/// <param name="error"> Optional error message </param>
		public static Task<IDomainResult<TValue>> CriticalDependencyErrorTask(string? error = null)	
																							=> Task.FromResult(CriticalDependencyError(error));
		
		#endregion // Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -
	}
}
