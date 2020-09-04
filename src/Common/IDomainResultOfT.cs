using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
		T Value { get; }

#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1
		static IDomainResult<T> Success(T value)						=> DomainResult<T>.Success(value);
		static IDomainResult<T> NotFound(string? message = null)		=> DomainResult<T>.NotFound(message);
		static IDomainResult<T> NotFound(IEnumerable<string> messages)	=> DomainResult<T>.NotFound(messages);
		static IDomainResult<T> Error(string? error = null)				=> DomainResult<T>.Error(error);
		static IDomainResult<T> Error(IEnumerable<string> errors)		=> DomainResult<T>.Error(errors);
		static IDomainResult<T> Error(IEnumerable<ValidationResult> validationResults) => DomainResult<T>.Error(validationResults);

		static Task<IDomainResult<T>> SuccessTask(T value)						=> DomainResult<T>.SuccessTask(value);
		static Task<IDomainResult<T>> NotFoundTask(string? message = null)		=> DomainResult<T>.NotFoundTask(message);
		static Task<IDomainResult<T>> NotFoundTask(IEnumerable<string> messages)=> DomainResult<T>.NotFoundTask(messages);
		static Task<IDomainResult<T>> ErrorTask(string? message = null)			=> DomainResult<T>.ErrorTask(message);
		static Task<IDomainResult<T>> ErrorTask(IEnumerable<string> errors)		=> DomainResult<T>.ErrorTask(errors);
		static Task<IDomainResult<T>> ErrorTask(IEnumerable<ValidationResult> validationResults) => DomainResult<T>.ErrorTask(validationResults);
#endif
	}
}
