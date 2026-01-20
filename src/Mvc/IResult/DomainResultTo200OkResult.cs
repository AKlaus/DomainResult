using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace DomainResults.Mvc;

public static partial class DomainResultExtensions
{
	//
	// Conversion to HTTP code 200 (OK) - IResult (the minimal API)
	//

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		For the minimal API only.
	/// </summary>
	/// <remarks>
	///		Uses <see cref="TypedResults"/> for better type safety and compile-time checking (available since .NET 7).
	/// </remarks>
	/// <typeparam name="T"> The returned value type from the domain operation in <paramref name="domainResult"/> </typeparam>
	/// <param name="domainResult"> Details of the operation results (<see cref="DomainResult{T}"/>) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static IResult ToResult<T>(this IDomainResult<T> domainResult,
									  Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
		=> ToResult(domainResult.Value, domainResult, errorAction, value => TypedResults.Ok(value));

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		For the minimal API only.
	///		The result is wrapped in a <see cref="Task"/>
	/// </summary>
	/// <remarks>
	///		Uses <see cref="TypedResults"/> for better type safety and compile-time checking (available since .NET 7).
	/// </remarks>
	/// <typeparam name="T"> The returned value type from the domain operation in <paramref name="domainResultTask"/> </typeparam>
	/// <param name="domainResultTask"> Details of the operation results (<see cref="DomainResult{T}"/>) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<IResult> ToResult<T>(this Task<IDomainResult<T>> domainResultTask,
	                                              Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
	{
		var domainResult = await domainResultTask;
		return ToResult(domainResult.Value, domainResult, errorAction, value => TypedResults.Ok(value));
	}

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		For the minimal API only.
	/// </summary>
	/// <remarks>
	///		Uses <see cref="TypedResults"/> for better type safety and compile-time checking (available since .NET 7).
	/// </remarks>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static IResult ToResult<V, R>(this (V, R) domainResult,
										 Action<ProblemDetails, R>? errorAction = null)
										 where R : IDomainResult
		=> domainResult.ToCustomResult(value => TypedResults.Ok(value), errorAction);

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		For the minimal API only.
	///		The result is wrapped in a <see cref="Task{T}"/>
	/// </summary>
	/// <remarks>
	///		Uses <see cref="TypedResults"/> for better type safety and compile-time checking (available since .NET 7).
	/// </remarks>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <param name="domainResultTask"> A task with returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<IResult> ToResult<V, R>(this Task<(V, R)> domainResultTask,
													 Action<ProblemDetails, R>? errorAction = null)
													 where R : IDomainResult
	{
		var domainResult = await domainResultTask;
		return domainResult.ToCustomResult(value => TypedResults.Ok(value), errorAction);
	}
}