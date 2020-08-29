using System;
using System.Threading.Tasks;

using DomainResults.Domain;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc
{
	public static partial class DomainResultExtensions
	{
		//
		// Conversion to HTTP code 200 (OK)
		//

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
	}
}