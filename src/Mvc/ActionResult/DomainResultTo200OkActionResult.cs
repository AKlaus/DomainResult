using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace DomainResults.Mvc;

public static partial class DomainResultExtensions
{
	//
	// Conversion to HTTP code 200 (OK) - ActionResult
	//

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error
	/// </summary>
	/// <typeparam name="T"> The returned value type from the domain operation in <paramref name="domainResult"/> </typeparam>
	/// <param name="domainResult"> Details of the operation results (<see cref="DomainResult{T}"/>) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static IActionResult ToActionResult<T>(this IDomainResult<T> domainResult,
												 Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
		=> ToActionResult(domainResult.Value, domainResult, errorAction, ToOkOrNoContentActionResult);

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		The result is wrapped in a <see cref="Task{V}"/>
	/// </summary>
	/// <typeparam name="T"> The returned value type from the domain operation in <paramref name="domainResultTask"/> </typeparam>
	/// <param name="domainResultTask"> Details of the operation results (<see cref="DomainResult{T}"/>) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<IActionResult> ToActionResult<T>(this Task<IDomainResult<T>> domainResultTask,
															  Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
	{
		var domainResult = await domainResultTask;
		return ToActionResult(domainResult.Value, domainResult, errorAction, ToOkOrNoContentActionResult);
	}

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error
	/// </summary>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static IActionResult ToActionResult<V, R>(this (V, R) domainResult,
													Action<ProblemDetails, R>? errorAction = null)
													where R : IDomainResult
		=> domainResult.ToCustomActionResult(ToOkOrNoContentActionResult, errorAction);

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		The result is wrapped in a <see cref="Task{T}"/>
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
		return domainResult.ToCustomActionResult(ToOkOrNoContentActionResult, errorAction);
	}

	/// <summary>
	///		Returns HTTP code 200 (OK) with the value, or 204 (NoContent) for a <c>null</c> value
	/// </summary>
	/// <remarks>
	///		A default value, such as 0 or false, is a value
	/// </remarks>
	private static ActionResult ToOkOrNoContentActionResult<V>([AllowNull] V value)
		=> value is null ? new NoContentResult() : new OkObjectResult(value);
}