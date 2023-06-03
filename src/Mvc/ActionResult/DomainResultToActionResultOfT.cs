using System;
using System.Collections.Generic;

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
			DomainOperationStatus.NotFound		 => SadResponse(HttpCodeConvention.NotFoundHttpCode,		HttpCodeConvention.NotFoundProblemDetailsTitle,	 	errorDetails, errorAction),
			DomainOperationStatus.Unauthorized	 => SadResponse(HttpCodeConvention.UnauthorizedHttpCode,	HttpCodeConvention.UnauthorizedProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Conflict		 => SadResponse(HttpCodeConvention.ConflictHttpCode,		HttpCodeConvention.ConflictProblemDetailsTitle,	 	errorDetails, errorAction),
			DomainOperationStatus.ContentTooLarge=> SadResponse(HttpCodeConvention.ContentTooLargeHttpCode,	HttpCodeConvention.ContentTooLargeProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Failed		 => SadResponse(HttpCodeConvention.FailedHttpCode,	 		HttpCodeConvention.FailedProblemDetailsTitle,		errorDetails, errorAction),
			DomainOperationStatus.CriticalDependencyError
												 => SadResponse(HttpCodeConvention.CriticalDependencyErrorHttpCode,HttpCodeConvention.CriticalDependencyErrorProblemDetailsTitle, errorDetails, errorAction),
			DomainOperationStatus.Success		 => EqualityComparer<V>.Default.Equals(value!, default!)
																	? new NoContentResult() as ActionResult // No value, means returning HTTP status 204
																	: valueToActionResultFunc(value),
			_ => throw new ArgumentOutOfRangeException(nameof(errorDetails)),
		};
}