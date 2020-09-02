using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc
{
	/// <summary>
	///     Converts domain response to <see cref="ActionResult" />
	/// </summary>
	public static partial class DomainResultExtensions
	{
		public static ActionResultConventions Conventions = new ActionResultConventions();

		/// <summary>
		///		Delegeate for a function to convert a value to <see cref="IActionResult"/>.
		///		It could have been replaced with 'Func<V, ActionResult>', but there is no way to enforce [AllowNull] on the generic Func declaration
		/// </summary>
		/// <typeparam name="V"> The returned value type (either a struct or a class) </typeparam>
		/// <typeparam name="T"> The returned action result type of <see cref="IActionResult"/> </typeparam>
		/// <param name="value"> The value to be returned </param>
		/// <returns> Action result </returns>
		public delegate T ValueToActionResultFunc<V, T>([AllowNull] V value) where T : IActionResult;

		private static ActionResult ToActionResult<V, R, TResult>((V, R) domainResult,
														Action<ProblemDetails, R>? errorAction,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc)
														where R : IDomainResultBase
														where TResult: ActionResult
			=> ToActionResult(domainResult.Item1, domainResult.Item2, errorAction, valueToActionResultFunc);

		private static ActionResult ToActionResult<V, R, TResult>([AllowNull] V value,
														R errorDetails,
														Action<ProblemDetails, R>? errorAction,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc)
														where R : IDomainResultBase
														where TResult : ActionResult
			=> errorDetails.Status switch
			{
				DomainOperationStatus.NotFound	=> SadResponse(Conventions.NotFoundHttpCode, Conventions.NotFoundProblemDetailsTitle, errorDetails, errorAction),
				DomainOperationStatus.Error		=> SadResponse(Conventions.ErrorHttpCode,	 Conventions.ErrorProblemDetailsTitle,	  errorDetails, errorAction),
				DomainOperationStatus.Success	=> EqualityComparer<V>.Default.Equals(value!, default!)
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
		/// <param name="messages"> A list of messages clarifying what's not found </param>
		private static ObjectResult SadResponse<R>(int statusCode, string title, R errorDetails, Action<ProblemDetails, R>? errorAction = null) where R : IDomainResultBase
		{
			var problemDetails = new ProblemDetails
			{
				Title = title,
				Detail = errorDetails?.Errors?.Any() == true ? string.Join(", ", errorDetails.Errors) : null,
				Status = statusCode
			};

			errorAction?.Invoke(problemDetails, errorDetails);

			return new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
		}
	}
}