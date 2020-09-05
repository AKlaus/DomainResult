using System;
using System.Collections.Generic;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc
{
#if !NETCOREAPP2_0
	//
	// Conversion to ActionResult<T> (the type exists starting from .NET Core 2.1 and not present in .NET Standard 2.0)
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
				DomainOperationStatus.NotFound	=> SadResponse(ActionResultConventions.NotFoundHttpCode, ActionResultConventions.NotFoundProblemDetailsTitle, errorDetails, errorAction),
				DomainOperationStatus.Error		=> SadResponse(ActionResultConventions.ErrorHttpCode,	 ActionResultConventions.ErrorProblemDetailsTitle,	  errorDetails, errorAction),
				DomainOperationStatus.Success	=> EqualityComparer<V>.Default.Equals(value!, default!)
																		? new NoContentResult() as ActionResult // No value, means returning HTTP status 204
																		: valueToActionResultFunc(value),
				_ => throw new ArgumentOutOfRangeException(),
			};
	}
#endif
}