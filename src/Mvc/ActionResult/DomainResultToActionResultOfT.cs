using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace DomainResults.Mvc;

//
// Conversion to ActionResult<T> (the type exists starting from .NET Core 2.1 and not present in earlier versions)
//
public static partial class DomainResultExtensions
{
	private static ActionResult<V> ToActionResultOfT<V, R> (V value,
															R errorDetails,
															Action<ProblemDetails, R>? errorAction,
															Func<V, ActionResult<V>> valueToActionResultFunc)
															where R : IDomainResultBase
		=> errorDetails.Status switch
		{
			DomainOperationStatus.NotFound		 => CreateProblemResponse(HttpCodeConvention.NotFoundHttpCode,		HttpCodeConvention.NotFoundProblemDetailsTitle,	 		errorDetails, errorAction),
			DomainOperationStatus.Unauthorized	 => CreateProblemResponse(HttpCodeConvention.UnauthorizedHttpCode,	HttpCodeConvention.UnauthorizedProblemDetailsTitle,		errorDetails, errorAction),
			DomainOperationStatus.Conflict		 => CreateProblemResponse(HttpCodeConvention.ConflictHttpCode,		HttpCodeConvention.ConflictProblemDetailsTitle,	 		errorDetails, errorAction),
			DomainOperationStatus.ContentTooLarge=> CreateProblemResponse(HttpCodeConvention.ContentTooLargeHttpCode,HttpCodeConvention.ContentTooLargeProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Failed		 => CreateProblemResponse(HttpCodeConvention.FailedHttpCode,	 	HttpCodeConvention.FailedProblemDetailsTitle,			errorDetails, errorAction),
			DomainOperationStatus.CriticalDependencyError
												 => CreateProblemResponse(HttpCodeConvention.CriticalDependencyErrorHttpCode,HttpCodeConvention.CriticalDependencyErrorProblemDetailsTitle, errorDetails, errorAction),
			DomainOperationStatus.Success		 => EqualityComparer<V>.Default.Equals(value!, default!)
																	? new NoContentResult() as ActionResult // No value, means returning HTTP status 204
																	: valueToActionResultFunc(value),
			_ => throw new ArgumentOutOfRangeException(nameof(errorDetails)),
		};
}