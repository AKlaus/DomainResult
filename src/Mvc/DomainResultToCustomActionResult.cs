using System;
using System.Threading.Tasks;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc
{
	public static partial class DomainResultExtensions
	{
		//
		// Conversion to custom HTTP codes (e.g. 201 Created) and objects (e.g. VirtualFileResult, FileStreamResult, etc.)
		//

		/// <summary>
		///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <typeparam name="TResult"> The result type returned in <paramref name="valueToActionResultFunc"/> if the domain operation was successful </typeparam>
		/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="valueToActionResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static IActionResult ToCustomActionResult<V, R, TResult>(this (V, R) domainResult,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, R>? errorAction = null)
														where R : IDomainResultBase
														where TResult : ActionResult
			=> ToActionResult(domainResult.Item1, domainResult.Item2, errorAction, valueToActionResultFunc);

		/// <summary>
		///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <typeparam name="TResult"> The result type returned in <paramref name="valueToActionResultFunc"/> if the domain operation was successful </typeparam>
		/// <param name="domainResultTask"> Returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="valueToActionResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<IActionResult> ToCustomActionResult<V, R, TResult>(this Task<(V, R)> domainResultTask,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, R>? errorAction = null)
														where R : IDomainResultBase
														where TResult : ActionResult
		{
			var domainResult = await domainResultTask;
			return ToActionResult(domainResult.Item1, domainResult.Item2, errorAction, valueToActionResultFunc); 
		}

		/// <summary>
		///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="TResult"> The result type returned in <paramref name="valueToActionResultFunc"/> if the domain operation was successful </typeparam>
		/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="valueToActionResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static IActionResult ToCustomActionResult<V, TResult>(this IDomainResult<V> domainResult,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, IDomainResult<V>>? errorAction = null)
														where TResult : ActionResult
			=> ToActionResult(domainResult.Value, domainResult, errorAction, valueToActionResultFunc);

		/// <summary>
		///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="TResult"> The result type returned in <paramref name="valueToActionResultFunc"/> if the domain operation was successful </typeparam>
		/// <param name="domainResultTask"> Returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="valueToActionResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<IActionResult> ToCustomActionResult<V, TResult>(this Task<IDomainResult<V>> domainResultTask,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, IDomainResult<V>>? errorAction = null)
														where TResult : ActionResult
		{
			var domainResult = await domainResultTask;
			return ToActionResult(domainResult.Value, domainResult, errorAction, valueToActionResultFunc); 
		}
	}
}