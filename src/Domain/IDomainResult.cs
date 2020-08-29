using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DomainResults.Domain
{
	public interface IDomainResult: IDomainResultBase
	{
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1
		static IDomainResult Success()								=> DomainResult.Success();
		static IDomainResult NotFound(string? message = null)		=> DomainResult.NotFound(message);
		static IDomainResult NotFound(IEnumerable<string> messages) => DomainResult.NotFound(messages);
		static IDomainResult Error(string? error = null)			=> DomainResult.Error(error);
		static IDomainResult Error(IEnumerable<string> errors)		=> DomainResult.Error(errors);
		static IDomainResult Error(IEnumerable<ValidationResult> validationResults) => DomainResult.Error(validationResults);

		static Task<IDomainResult> SuccessTask()								=> DomainResult.SuccessTask();
		static Task<IDomainResult> NotFoundTask(string? message = null)			=> DomainResult.NotFoundTask(message);
		static Task<IDomainResult> NotFoundTask(IEnumerable<string> messages)	=> DomainResult.NotFoundTask(messages);
		static Task<IDomainResult> ErrorTask(string? message = null)			=> DomainResult.ErrorTask(message);
		static Task<IDomainResult> ErrorTask(IEnumerable<string> errors)		=> DomainResult.ErrorTask(errors);
		static Task<IDomainResult> ErrorTask(IEnumerable<ValidationResult> validationResults) => DomainResult.ErrorTask(validationResults);

		static (TValue, IDomainResult) Success<TValue>(TValue value)				 => (value, DomainResult.Success());
		static (TValue, IDomainResult) NotFound<TValue>(string? message = null)		 => (default, DomainResult.NotFound(message));
		static (TValue, IDomainResult) NotFound<TValue>(IEnumerable<string> messages)=> (default, DomainResult.NotFound(messages));
		static (TValue, IDomainResult) Error<TValue>(string? error = null)			 => (default, DomainResult.Error(error));
		static (TValue, IDomainResult) Error<TValue>(IEnumerable<string> errors)	 => (default, DomainResult.Error(errors));
		static (TValue, IDomainResult) Error<TValue>(IEnumerable<ValidationResult> validationResults) 
																					 => (default, DomainResult.Error(validationResults));

		static Task<(TValue, IDomainResult)> SuccessTask<TValue>(TValue value)				   => Task.FromResult(Success(value));
		static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(string? message = null)	   => Task.FromResult(NotFound<TValue>(message));
		static Task<(TValue, IDomainResult)> NotFoundTask<TValue>(IEnumerable<string> messages)=> Task.FromResult(NotFound<TValue>(messages));
		static Task<(TValue, IDomainResult)> ErrorTask<TValue>(string? message = null)		   => Task.FromResult(Error<TValue>(message));
		static Task<(TValue, IDomainResult)> ErrorTask<TValue>(IEnumerable<string> errors)	   => Task.FromResult(Error<TValue>(errors));
		static Task<(TValue, IDomainResult)> ErrorTask<TValue>(IEnumerable<ValidationResult> validationResults) 
																									  => Task.FromResult(Error<TValue>(validationResults));
#endif
	}
}