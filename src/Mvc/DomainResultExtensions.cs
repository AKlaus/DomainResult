using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using AK.DomainResults.Domain;

using Microsoft.AspNetCore.Mvc;

namespace AK.DomainResults.Mvc
{
	/// <summary>
	///     Converts domain response to <see cref="ActionResult" />
	/// </summary>
	public static class DomainResultExtensions
	{
		/// <summary>
		///		Delegeate for a function to convert a value to <see cref="IActionResult"/>.
		///		It could have been replaced with 'Func<V, ActionResult>', but there is no way to enforce [AllowNull] on the generic Func declaration
		/// </summary>
		/// <typeparam name="V"> The returned value type (either a struct or a class) </typeparam>
		/// <typeparam name="T"> The returned action result type of <see cref="IActionResult"/> </typeparam>
		/// <param name="value"> The value to be returned </param>
		/// <returns> Action result </returns>
		public delegate T ValueToActionResultFunc<V, T>([AllowNull] V value) where T : IActionResult;

		#region HTTP code 204 (NoContent) [PUBLIC, STATIC] --------------------

		/// <summary>
		///		Returns HTTP code 204 (NoContent) or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResult"> Details of the operation results </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static ActionResult ToActionResult<R>(this R domainResult,
													 Action<ProblemDetails, R>? errorAction = null)
													 where R : IDomainResult
			=> ToActionResult<object, R, NoContentResult>(null, domainResult, errorAction, (value) => new NoContentResult());

		/// <summary>
		///		Returns a task with HTTP code 204 (NoContent) or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResultBase"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResultTask"> A task with details of the operation results </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<IActionResult> ToActionResult<R>(this Task<R> domainResultTask,
																  Action<ProblemDetails, R>? errorAction = null)
																  where R : IDomainResult
			=> ToActionResult<object, R, NoContentResult>(null, await domainResultTask, errorAction, (value) => new NoContentResult());

		#endregion // HTTP code 204 (NoContent) [PUBLIC, STATIC] --------------

		#region HTTP code 200 (OK) [PUBLIC, STATIC] ---------------------------

		/// <summary>
		///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="T"> The type derived from <see cref="IDomainResult{V}"/>, e.g. <see cref="DomainResult{V}"/> </typeparam>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <param name="domainResult"> Details of the operation results </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static ActionResult ToActionResult<T>(this IDomainResult<T> domainResult,
													 Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
			=> ToActionResult(domainResult.Value, domainResult, errorAction, (value) => new OkObjectResult(value));

		/// <summary>
		///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="T"> The type derived from <see cref="IDomainResult{V}"/>, e.g. <see cref="DomainResult{V}"/> </typeparam>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <param name="domainResultTask"> A task with details of the operation results </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<IActionResult> ToActionResult<T>(this Task<IDomainResult<T>> domainResultTask,
																  Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
		{
			var domainResult = await domainResultTask;
			return ToActionResult(domainResult.Value, domainResult, errorAction, (value) => new OkObjectResult(value));
		}

		/// <summary>
		///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static ActionResult ToActionResult<V, R>(this (V, R) domainResult,
														Action<ProblemDetails, R>? errorAction = null)
														where R : IDomainResult
			=> ToActionResult(domainResult, errorAction, (value) => new OkObjectResult(value));

		/// <summary>
		///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResultTask"> A task with returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<IActionResult> ToActionResult<V, R>(this Task<(V, R)> domainResultTask,
																	 Action<ProblemDetails, R>? errorAction = null)
																	 where R : IDomainResult
		{
			var domainResult = await domainResultTask;
			return ToActionResult(domainResult, errorAction, (value) => new OkObjectResult(value));
		}

		#endregion // HTTP code 200 (OK) [PUBLIC, STATIC] ---------------------

		#region HTTP code 201 (Created) [PUBLIC, STATIC] ----------------------

		public static ActionResult ToCreatedResult<T, V, R>(this T domainResult,
														 string location,
														 Action<ProblemDetails, R>? errorAction = null)
														 where T : Tuple<V, R>
														 where R : IDomainResult
			=> ToActionResult((domainResult.Item1, domainResult.Item2), errorAction, (value) => new CreatedResult(location, value));

		public static ActionResult ToCreatedResult<T, V, R>(this T domainResult,
														 Uri location,
														 Action<ProblemDetails, R>? errorAction = null)
														 where T : Tuple<V, R>
														 where R : IDomainResult
			=> ToActionResult((domainResult.Item1, domainResult.Item2), errorAction, (value) => new CreatedResult(location, value));

		public static ActionResult ToCreatedAtActionResult<T, V, R>(this T domainResult,
																 ValueToActionResultFunc<V, CreatedAtActionResult> valueToActionResultFunc,
																 Action<ProblemDetails, R>? errorAction = null)
																 where T : Tuple<V, R>
																 where R : IDomainResult
			=> ToActionResult((domainResult.Item1, domainResult.Item2), errorAction, valueToActionResultFunc);

		#endregion // HTTP code 201 (Created) [PUBLIC, STATIC] ----------------

		#region Auxiliary methods [PRIVATE, STATIC] ---------------------------

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
				DomainOperationStatus.NotFound	=> SadResponse(404, "Not Found",   errorDetails, errorAction),
				DomainOperationStatus.Error		=> SadResponse(400, "Bad Request", errorDetails, errorAction),  // Can be a '422'. Opinions: https://stackoverflow.com/a/52098667/968003, https://stackoverflow.com/a/20215807/968003
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

		#endregion // Auxiliary methods [PRIVATE, STATIC] ---------------------
	}
}