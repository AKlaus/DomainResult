using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace DomainResults.Mvc;

/// <summary>
///     Converts domain response to <see cref="ActionResult" /> or <see cref="IResult" /> response for the WebAPI
/// </summary>
public static partial class DomainResultExtensions
{
	/// <summary>
	///		Delegate for a function to convert a value to <see cref="IActionResult"/>.
	/// </summary>
	/// <remarks>
	///		It could have been replaced with 'Func{V, ActionResult}', but there is no way to enforce [AllowNull] on the generic Func declaration
	/// </remarks>
	/// <typeparam name="V"> The returned value type (either a struct or a class) </typeparam>
	/// <typeparam name="T"> The returned action result type of <see cref="IActionResult"/> </typeparam>
	/// <param name="value"> The value to be returned </param>
	/// <returns> Action result </returns>
	public delegate T ValueToActionResultFunc<V, out T>([AllowNull] V value) where T : IActionResult;

	private static ActionResult ToActionResult<V, R, TResult>([AllowNull] V value,
	                                                          R errorDetails,
	                                                          Action<ProblemDetails, R>? errorAction,
	                                                          ValueToActionResultFunc<V, TResult> valueToActionResultFunc)
																where R : IDomainResultBase
																where TResult : ActionResult
		=> errorDetails.Status switch
		{
			DomainOperationStatus.NotFound		 => SadResponse(HttpCodeConvention.NotFoundHttpCode,	HttpCodeConvention.NotFoundProblemDetailsTitle,		errorDetails, errorAction),
			DomainOperationStatus.Unauthorized	 => SadResponse(HttpCodeConvention.UnauthorizedHttpCode,HttpCodeConvention.UnauthorizedProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Conflict		 => SadResponse(HttpCodeConvention.ConflictHttpCode,	HttpCodeConvention.ConflictProblemDetailsTitle,		errorDetails, errorAction),
			DomainOperationStatus.ContentTooLarge=> SadResponse(HttpCodeConvention.ContentTooLargeHttpCode,	HttpCodeConvention.ContentTooLargeProblemDetailsTitle,		errorDetails, errorAction),
			DomainOperationStatus.Failed		 => SadResponse(HttpCodeConvention.FailedHttpCode,		HttpCodeConvention.FailedProblemDetailsTitle,			errorDetails, errorAction),
			DomainOperationStatus.CriticalDependencyError
												 => SadResponse(HttpCodeConvention.CriticalDependencyErrorHttpCode,	HttpCodeConvention.CriticalDependencyErrorProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Success		 => EqualityComparer<V>.Default.Equals(value!, default!)
																	? new NoContentResult() as ActionResult // No value, means returning HTTP status 204
																	: valueToActionResultFunc(value),
			_ => throw new ArgumentOutOfRangeException(),
		};

	/// <summary>
	///		Return 4xx status with a machine-readable format for specifying errors based on https://tools.ietf.org/html/rfc7807.
	/// </summary>
	/// <remarks>
	///		Alternatively can simply return <seealso cref="NotFoundResult"/> or <seealso cref="BadRequestObjectResult"/> without a JSON
	/// </remarks>
	private static ObjectResult SadResponse<R>(int statusCode, string title, R? errorDetails, Action<ProblemDetails, R>? errorAction = null) where R : IDomainResultBase
	{
		var problemDetails = new ProblemDetails
			{
				Title = title,
				Detail = errorDetails?.Errors.Any() == true ? string.Join(", ", errorDetails.Errors) : null,
				Status = statusCode
			};

		errorAction?.Invoke(problemDetails, errorDetails!);

		return new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
	}
}