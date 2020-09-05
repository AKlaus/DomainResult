using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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
		public TValue Value { get; }

		#region Constructors [PROTECTED] --------------------------------------

		/// <summary>
		///		Creates a new instance with a 'error'/'not found' status
		/// </summary>
		/// <param name="errorDetails"> Error details described in <see cref="IDomainResult"/> </param>
		protected DomainResult(IDomainResult errorDetails)				: this(default!, errorDetails) { }
		/// <summary>
		///		Creates a new instance with 'success' status and a value
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		protected DomainResult(TValue value)							: this(value, DomainResult.Success()) {}
		/// <summary>
		///		The most generic constructor. Creates a new instance with a specified status and error meesages
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		/// <param name="errorDetails"> Error details described in <see cref="IDomainResult"/> </param>
		protected DomainResult(TValue value, IDomainResult errorDetails)
		{
			Value = value;
			_status = errorDetails;
		}
		#endregion // Constructors [PROTECTED] --------------------------------

		/// <summary>
		///		Implictly converts the specified <paramref name="value"/> to an <see cref="DomainResult{TValue}"/>
		/// </summary>
		/// <param name="value"> The parameter for conversion </param>
		public static implicit operator DomainResult<TValue>(TValue value) => new DomainResult<TValue>(value);

		// TODO: Consider to depricate the extension methods in this class (below) in favour of ones in 'DomainResult'

		#region Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------------

		/// <summary>
		///		Get 'success' status with a value. Later it can be converted to HTTP code 200 (Ok)
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		public static IDomainResult<TValue> Success(TValue value)					=> new DomainResult<TValue>(value);

		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		public static IDomainResult<TValue> NotFound(string? message = null)		=> new DomainResult<TValue>(DomainResult.NotFound(message));
		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		public static IDomainResult<TValue> NotFound(IEnumerable<string> messages)	=> new DomainResult<TValue>(DomainResult.NotFound(messages));

		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		public static IDomainResult<TValue> Error(string? error = null)				=> new DomainResult<TValue>(DomainResult.Error(error));
		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		public static IDomainResult<TValue> Error(IEnumerable<string> errors)		=> new DomainResult<TValue>(DomainResult.Error(errors));
		/// <summary>
		///		Get 'error' status with validation errors. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		public static IDomainResult<TValue> Error(IEnumerable<ValidationResult> validationResults) => new DomainResult<TValue>(DomainResult.Error(validationResults));

		#endregion // Extensions of 'IDomainResult<T>' [STATIC, PUBLIC] -------

		#region Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -------

		/// <summary>
		///		Get 'success' status with a value (all wrapped in <see cref="Task{T}"/>).
		///		Later it can be converted to HTTP code 200 (Ok)
		/// </summary>
		/// <param name="value"> The value to be returned </param>
		public static Task<IDomainResult<TValue>> SuccessTask(TValue value)					=> Task.FromResult(Success(value));

		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult<TValue>> NotFoundTask(string? message = null)		=> Task.FromResult(NotFound(message));
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		public static Task<IDomainResult<TValue>> NotFoundTask(IEnumerable<string> messages)=> Task.FromResult(NotFound(messages));

		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		public static Task<IDomainResult<TValue>> ErrorTask(string? error = null)			=> Task.FromResult(Error(error));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		public static Task<IDomainResult<TValue>> ErrorTask(IEnumerable<string> errors)		=> Task.FromResult(Error(errors));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		public static Task<IDomainResult<TValue>> ErrorTask(IEnumerable<ValidationResult> validationResults)
																							=> Task.FromResult(Error(validationResults));
		#endregion // Extensions of 'Task<IDomainResult<T>>' [STATIC, PUBLIC] -
	}
}
