using System;
using System.Collections.Generic;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

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
			DomainOperationStatus.NotFound		=> SadResponse(ActionResultConventions.NotFoundHttpCode,	ActionResultConventions.NotFoundProblemDetailsTitle,	 errorDetails, errorAction),
			DomainOperationStatus.Unauthorized	=> SadResponse(ActionResultConventions.UnauthorizedHttpCode,ActionResultConventions.UnauthorizedProblemDetailsTitle, errorDetails, errorAction),
			DomainOperationStatus.Conflict		=> SadResponse(ActionResultConventions.ConflictHttpCode,	ActionResultConventions.ConflictProblemDetailsTitle,	 errorDetails, errorAction),
			DomainOperationStatus.Failed		=> SadResponse(ActionResultConventions.FailedHttpCode,	 	ActionResultConventions.FailedProblemDetailsTitle,		 errorDetails, errorAction),
			DomainOperationStatus.CriticalDependencyError
												=> SadResponse(ActionResultConventions.CriticalDependencyErrorHttpCode,ActionResultConventions.CriticalDependencyErrorProblemDetailsTitle, errorDetails, errorAction),
			DomainOperationStatus.Success		=> EqualityComparer<V>.Default.Equals(value!, default!)
																	? new NoContentResult() as ActionResult // No value, means returning HTTP status 204
																	: valueToActionResultFunc(value),
			_ => throw new ArgumentOutOfRangeException(nameof(errorDetails)),
		};
}