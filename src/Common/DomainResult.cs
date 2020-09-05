using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DomainResults.Common
{
	/// <inheritdoc/>
	public partial class DomainResult : IDomainResult
	{
		/// <inheritdoc/>
		public DomainOperationStatus Status { get; }
		/// <inheritdoc/>
		public IReadOnlyCollection<string> Errors { get; }
		/// <inheritdoc/>
		public bool IsSuccess => Status == DomainOperationStatus.Success;

		#region Constructors [PUBLIC, PROTECTED] ------------------------------

		/// <summary>
		///		Creates a new instance with 'success' status
		/// </summary>
		protected DomainResult()														: this(DomainOperationStatus.Success, string.Empty) { }
		/// <summary>
		///		Creates a new instance with a specified status and error meesages
		/// </summary>
		/// <param name="status"> The state </param>
		/// <param name="error"> Optional cutom error messages </param>
		protected DomainResult(DomainOperationStatus status, string? error)				: this(status, (!string.IsNullOrEmpty(error) ? new[] { error } : new string[0])!) { }
		/// <summary>
		///		Creates a new instance with a 'error'/'not found' status and error meesages
		/// </summary>
		/// <param name="status"> The state </param>
		/// <param name="errors"> Cutom error messages </param>
		public DomainResult(DomainOperationStatus status, IEnumerable<string> errors)
		{
			Status = status;
			Errors = errors.ToArray();
		}
		/// <summary>
		///		Creates a new instance with 'error' status and validation error meesages
		/// </summary>
		/// <param name="validationResults"> Validation error meesages </param>
		protected DomainResult(IEnumerable<ValidationResult> validationResults)
		{
			Status = DomainOperationStatus.Error;
			Errors = (from message in validationResults
					  select $"{message.ErrorMessage}{(message.MemberNames?.Any() == true ? " (" + string.Join(", ", message.MemberNames) + ")" : "")}"
					 ).ToArray();
		}
		#endregion // Constructors [PUBLIC, PROTECTED] ------------------------

		#region Extensions of 'IDomainResult' [STATIC, PUBLIC] ----------------

		/// <summary>
		///		Get 'success' status. Later it can be converted to HTTP code 204 (NoContent)
		/// </summary>
		public static IDomainResult Success()							 => new DomainResult();
		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		public static IDomainResult NotFound(string? message = null)	 => new DomainResult(DomainOperationStatus.NotFound, message);
		/// <summary>
		///		Get 'not found' status. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		public static IDomainResult NotFound(IEnumerable<string> messages)=> new DomainResult(DomainOperationStatus.NotFound, messages);
		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		public static IDomainResult Error(string? error = null)			 => new DomainResult(DomainOperationStatus.Error, error);
		/// <summary>
		///		Get 'error' status. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		public static IDomainResult Error(IEnumerable<string> errors)	 => new DomainResult(DomainOperationStatus.Error, errors);
		/// <summary>
		///		Get 'error' status with validation errors. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		public static IDomainResult Error(IEnumerable<ValidationResult> validationResults) => new DomainResult(validationResults);

		#endregion // Extensions of 'IDomainResult' [STATIC, PUBLIC] ----------

		#region Extensions of 'Task<IDomainResult>' [STATIC, PUBLIC] ----------

		/// <summary>
		///		Get 'success' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 204 (NoContent)
		/// </summary>
		public static Task<IDomainResult> SuccessTask()								=> Task.FromResult(Success());
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="message"> Optional message </param>
		public static Task<IDomainResult> NotFoundTask(string? message = null)		=> Task.FromResult(NotFound(message));
		/// <summary>
		///		Get 'not found' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 404 (NotFound)
		/// </summary>
		/// <param name="messages"> Custom messages </param>
		public static Task<IDomainResult> NotFoundTask(IEnumerable<string> messages)=> Task.FromResult(NotFound(messages));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="error"> Optional message </param>
		public static Task<IDomainResult> ErrorTask(string? error = null)			=> Task.FromResult(Error(error));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="errors"> Custom messages </param>
		public static Task<IDomainResult> ErrorTask(IEnumerable<string> errors)		=> Task.FromResult(Error(errors));
		/// <summary>
		///		Get 'error' status wrapped in a <see cref="Task{T}"/>. Later it can be converted to HTTP code 400/422
		/// </summary>
		/// <param name="validationResults"> Results of a validation request </param>
		public static Task<IDomainResult> ErrorTask(IEnumerable<ValidationResult> validationResults) => Task.FromResult(Error(validationResults));

		#endregion // Extensions of 'Task<IDomainResult>' [STATIC, PUBLIC] ----
	}
}