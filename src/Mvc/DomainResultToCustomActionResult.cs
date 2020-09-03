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

		public static ActionResult ToCustomActionResult<V, R, TResult>(this (V, R) domainResult,
														ValueToActionResultFunc<V, TResult> valueToActionResultFunc,
														Action<ProblemDetails, R>? errorAction = null)
														where R : IDomainResultBase
														where TResult : ActionResult
			=> ToActionResult(domainResult.Item1, domainResult.Item2, errorAction, valueToActionResultFunc);

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