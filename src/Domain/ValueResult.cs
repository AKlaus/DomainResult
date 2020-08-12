using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AK.DomainResults.Domain
{
	public static class ValueResult
	{
		public static (TValue, IDomainResult) Success<TValue>(TValue value)					=> DomainResult<TValue>.Success(value)		.ToValueTuple();
		public static (TValue, IDomainResult) NotFound<TValue>(string? message = null)		=> DomainResult<TValue>.NotFound(message)	.ToValueTuple();
		public static (TValue, IDomainResult) NotFound<TValue>(IEnumerable<string> messages)=> DomainResult<TValue>.NotFound(messages)	.ToValueTuple();
		public static (TValue, IDomainResult) Error<TValue>(string? message = null)			=> DomainResult<TValue>.Error(message)		.ToValueTuple();
		public static (TValue, IDomainResult) Error<TValue>(IEnumerable<string> errors)		=> DomainResult<TValue>.Error(errors)		.ToValueTuple();
		public static (TValue, IDomainResult) Error<TValue>(IEnumerable<ValidationResult> validationResults) 
																							=> DomainResult<TValue>.Error(validationResults).ToValueTuple();

		public static Task<(TValue, IDomainResult)> SuccessTask<TValue>(TValue value)				  => Task.FromResult(Success(value));
		public static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(string? message = null)	  => Task.FromResult(NotFound<TValue>(message));
		public static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(IEnumerable<string> messages)=> Task.FromResult(NotFound<TValue>(messages));
		public static Task<(TValue, IDomainResult)> ErrorTask<TValue>(string? message = null)		  => Task.FromResult(Error<TValue>(message));
		public static Task<(TValue, IDomainResult)> ErrorTask<TValue>(IEnumerable<string> errors)	  => Task.FromResult(Error<TValue>(errors));
		public static Task<(TValue, IDomainResult)> ErrorTask<TValue>(IEnumerable<ValidationResult> validationResults) 
																									  => Task.FromResult(Error<TValue>(validationResults));
	}
}
