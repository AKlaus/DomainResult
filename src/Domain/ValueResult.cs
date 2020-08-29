using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DomainResults.Domain
{
	public static class ValueResult
	{
		public static (TValue, IDomainResult) Success<TValue>(TValue value)					=> (value, DomainResult.Success());
		public static (TValue, IDomainResult) NotFound<TValue>(string? message = null)		=> (default, DomainResult.NotFound(message));
		public static (TValue, IDomainResult) NotFound<TValue>(IEnumerable<string> messages)=> (default, DomainResult.NotFound(messages));
		public static (TValue, IDomainResult) Error<TValue>(string? error = null)			=> (default, DomainResult.Error(error));
		public static (TValue, IDomainResult) Error<TValue>(IEnumerable<string> errors)		=> (default, DomainResult.Error(errors));
		public static (TValue, IDomainResult) Error<TValue>(IEnumerable<ValidationResult> validationResults) 
																							=> (default, DomainResult.Error(validationResults));

		public static Task<(TValue, IDomainResult)> SuccessTask<TValue>(TValue value)				  => Task.FromResult(Success(value));
		public static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(string? message = null)	  => Task.FromResult(NotFound<TValue>(message));
		public static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(IEnumerable<string> messages)=> Task.FromResult(NotFound<TValue>(messages));
		public static Task<(TValue, IDomainResult)> ErrorTask<TValue>(string? message = null)		  => Task.FromResult(Error<TValue>(message));
		public static Task<(TValue, IDomainResult)> ErrorTask<TValue>(IEnumerable<string> errors)	  => Task.FromResult(Error<TValue>(errors));
		public static Task<(TValue, IDomainResult)> ErrorTask<TValue>(IEnumerable<ValidationResult> validationResults) 
																									  => Task.FromResult(Error<TValue>(validationResults));
	}
}
