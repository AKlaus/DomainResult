using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace DomainResults.Mvc;

//
// Converts domain response to `IResult` for the minimal API
//
public static partial class DomainResultExtensions
{
	/// <summary>
	///		Delegate for a function to convert a value to <see cref="IResult"/> for the minimal API.
	/// </summary>
	/// <remarks>
	///		It could have been replaced with 'Func{V, IResult}', but there is no way to enforce [AllowNull] on the generic Func declaration
	/// </remarks>
	/// <typeparam name="V"> The returned value type (either a struct or a class) </typeparam>
	/// <typeparam name="T"> The returned action result type of <see cref="IResult"/> </typeparam>
	/// <param name="value"> The value to be returned </param>
	/// <returns> <see cref="IResult"/> for the minimal API </returns>
	public delegate T ValueToResultFunc<V, out T>([AllowNull] V value) where T : IResult;

	private static IResult ToResult<V, R, TResult>([AllowNull] V value,
	                                                R errorDetails,
	                                                Action<ProblemDetails, R>? errorAction,
	                                                ValueToResultFunc<V, TResult> valueToResultFunc)
														where R : IDomainResultBase
														where TResult : IResult
		=> errorDetails.Status switch
		{
			DomainOperationStatus.NotFound		 => CreateProblemResult(HttpCodeConvention.NotFoundHttpCode,		HttpCodeConvention.NotFoundProblemDetailsTitle,			errorDetails, errorAction),
			DomainOperationStatus.Unauthorized	 => CreateProblemResult(HttpCodeConvention.UnauthorizedHttpCode,	HttpCodeConvention.UnauthorizedProblemDetailsTitle,		errorDetails, errorAction),
			DomainOperationStatus.Conflict		 => CreateProblemResult(HttpCodeConvention.ConflictHttpCode,		HttpCodeConvention.ConflictProblemDetailsTitle,			errorDetails, errorAction),
			DomainOperationStatus.ContentTooLarge=> CreateProblemResult(HttpCodeConvention.ContentTooLargeHttpCode,	HttpCodeConvention.ContentTooLargeProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Failed		 => CreateProblemResult(HttpCodeConvention.FailedHttpCode,			HttpCodeConvention.FailedProblemDetailsTitle,			errorDetails, errorAction),
			DomainOperationStatus.CriticalDependencyError
												 => CreateProblemResult(HttpCodeConvention.CriticalDependencyErrorHttpCode,	HttpCodeConvention.CriticalDependencyErrorProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Success		 => EqualityComparer<V>.Default.Equals(value!, default!)
																	? TypedResults.NoContent()			// No value, means returning HTTP status 204. Since .NET 7 `TypedResults` provides better type safety
																	: valueToResultFunc(value),
			_ => throw new ArgumentOutOfRangeException(),
		};

	/// <summary>
	///		Creates a ProblemDetails response with a 4xx status for error scenarios based on RFC 7807.
	/// </summary>
	private static ProblemHttpResult CreateProblemResult<R>(int statusCode, string title, R? errorDetails, Action<ProblemDetails, R>? errorAction = null) where R : IDomainResultBase
	{
		var problemDetails = new ProblemDetails
			{
				Title = title,
				Detail = errorDetails?.Errors.Any() == true ? string.Join(", ", errorDetails.Errors) : null,
				Status = statusCode
			};
		errorAction?.Invoke(problemDetails, errorDetails!);

		// Since .NET 7 `TypedResults.Problem` is preferred over a more generic `Results.Problem`
		return TypedResults.Problem(problemDetails);
	}
}