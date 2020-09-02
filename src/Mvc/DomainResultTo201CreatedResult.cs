using System;
using System.Threading.Tasks;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc
{
	public static partial class DomainResultExtensions
	{
		//
		// Conversion to HTTP code 201 (Created)
		//

		public static ActionResult ToCreatedResult<V, R>(this (V, R) domainResult,
														 string location,
														 Action<ProblemDetails, R>? errorAction = null)
														 where R : IDomainResult
			=> ToActionResult((domainResult.Item1, domainResult.Item2), errorAction, (value) => new CreatedResult(location, value));
		public static async Task<ActionResult> ToCreatedResult<V, R>(this Task<(V, R)> domainResultTask,
														string location,
														Action<ProblemDetails, R>? errorAction = null)
														where R : IDomainResult
		{
			var domainResult = await domainResultTask;
			return ToActionResult((domainResult.Item1, domainResult.Item2), errorAction, (value) => new CreatedResult(location, value));
		}

		public static ActionResult ToCreatedResult<T>(this IDomainResult<T> domainResult,
													  string location,
													  Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
			=> ToActionResult((domainResult.Value, domainResult), errorAction, (value) => new CreatedResult(location, value));
		public static async Task<ActionResult> ToCreatedResult<T>(this Task<IDomainResult<T>> domainResultTask,
													  string location,
													  Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
		{
			var domainResult = await domainResultTask;
			return ToActionResult((domainResult.Value, domainResult), errorAction, (value) => new CreatedResult(location, value));
		}

		public static ActionResult ToCreatedResult<V, R>(this (V, R) domainResult,
														 Uri location,
														 Action<ProblemDetails, R>? errorAction = null)
														 where R : IDomainResult
			=> ToActionResult((domainResult.Item1, domainResult.Item2), errorAction, (value) => new CreatedResult(location, value));
		public static async Task<ActionResult> ToCreatedResult<V, R>(this Task<(V, R)> domainResultTask,
														Uri location,
														Action<ProblemDetails, R>? errorAction = null)
														where R : IDomainResult
		{
			var domainResult = await domainResultTask;
			return ToActionResult((domainResult.Item1, domainResult.Item2), errorAction, (value) => new CreatedResult(location, value));
		}

		public static ActionResult ToCreatedResult<T>(this IDomainResult<T> domainResult,
													  Uri location,
													  Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
			=> ToActionResult((domainResult.Value, domainResult), errorAction, (value) => new CreatedResult(location, value));
		public static async Task<ActionResult> ToCreatedResult<T>(this Task<IDomainResult<T>> domainResultTask,
													  Uri location,
													  Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
		{
			var domainResult = await domainResultTask;
			return ToActionResult((domainResult.Value, domainResult), errorAction, (value) => new CreatedResult(location, value));
		}

		public static ActionResult ToCreatedAtActionResult<T, V, R>(this T domainResult,
																 ValueToActionResultFunc<V, CreatedAtActionResult> valueToActionResultFunc,
																 Action<ProblemDetails, R>? errorAction = null)
																 where T : Tuple<V, R>
																 where R : IDomainResult
			=> ToActionResult((domainResult.Item1, domainResult.Item2), errorAction, valueToActionResultFunc);

	}
}