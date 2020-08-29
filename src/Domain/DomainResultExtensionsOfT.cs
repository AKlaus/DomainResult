using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DomainResults.Domain
{
	public partial class DomainResult
	{
		// Aliases for DomainResult<TValue> extensions [PUBLIC, STATIC]

		public static IDomainResult<TValue> Success<TValue>(TValue value)					=> DomainResult<TValue>.Success(value);
		public static IDomainResult<TValue> NotFound<TValue>(string? message = null)		=> DomainResult<TValue>.NotFound(message);
		public static IDomainResult<TValue> NotFound<TValue>(IEnumerable<string> messages)	=> DomainResult<TValue>.NotFound(messages);
		public static IDomainResult<TValue> Error<TValue>(string? message = null)			=> DomainResult<TValue>.Error(message);
		public static IDomainResult<TValue> Error<TValue>(IEnumerable<string> errors)		=> DomainResult<TValue>.Error(errors);
		public static IDomainResult<TValue> Error<TValue>(IEnumerable<ValidationResult> validationResults) => DomainResult<TValue>.Error(validationResults);

		public static Task<IDomainResult<TValue>> SuccessTask<TValue>(TValue value)					=> DomainResult<TValue>.SuccessTask(value);
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(string? message = null)		=> DomainResult<TValue>.NotFoundTask(message);
		public static Task<IDomainResult<TValue>> NotFoundTask<TValue>(IEnumerable<string> messages)=> DomainResult<TValue>.NotFoundTask(messages);
		public static Task<IDomainResult<TValue>> ErrorTask<TValue>(string? message = null)			=> DomainResult<TValue>.ErrorTask(message);
		public static Task<IDomainResult<TValue>> ErrorTask<TValue>(IEnumerable<string> errors)		=> DomainResult<TValue>.ErrorTask(errors);
		public static Task<IDomainResult<TValue>> ErrorTask<TValue>(IEnumerable<ValidationResult> validationResults) => DomainResult<TValue>.ErrorTask(validationResults);
	}
}
