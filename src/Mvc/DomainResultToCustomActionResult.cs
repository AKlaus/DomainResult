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
		///		Custom convertion of the domain result to whatever <see cref="ActionResult"/> type is required
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <typeparam name="TResult"> Type of the result returned in <paramref name="valueToActionResultFunc"/> if the domain operation was successfule </typeparam>
		/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="valueToActionResultFunc"></param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static ActionResult ToCustomActionResult<V, R, TResult>(this (V, R) domainResult,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, R>? errorAction = null)
														where R : IDomainResultBase
														where TResult : ActionResult
			=> ToActionResult(domainResult.Item1, domainResult.Item2, errorAction, valueToActionResultFunc);

		/// <summary>
		///		Custom convertion of the domain result to whatever <see cref="ActionResult"/> type is required
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <typeparam name="TResult"> Type of the result returned in <paramref name="valueToActionResultFunc"/> if the domain operation was successfule </typeparam>
		/// <param name="domainResultTask"> Returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="valueToActionResultFunc"></param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<ActionResult> ToCustomActionResult<V, R, TResult>(this Task<(V, R)> domainResultTask,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, R>? errorAction = null)
														where R : IDomainResultBase
														where TResult : ActionResult
		{
			var domainResult = await domainResultTask;
			return ToActionResult(domainResult.Item1, domainResult.Item2, errorAction, valueToActionResultFunc); 
		}

		public static IActionResult ToCustomActionResult<V, TResult>(this IDomainResult<V> domainResult,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, IDomainResult<V>>? errorAction = null)
														where TResult : ActionResult
			=> ToActionResult(domainResult.Value, domainResult, errorAction, valueToActionResultFunc);
		public static async Task<ActionResult> ToCustomActionResult<V, TResult>(this Task<IDomainResult<V>> domainResultTask,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, IDomainResult<V>>? errorAction = null)
														where TResult : ActionResult
		{
			var domainResult = await domainResultTask;
			return ToActionResult(domainResult.Value, domainResult, errorAction, valueToActionResultFunc); 
		}
	}
}