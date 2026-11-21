using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

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
	///		The return type gives OpenAPI the 200 (OK) response with the value type, and the 204 (NoContent) response.
	///		Problem responses still need <c>.ProducesProblem()</c>, because <see cref="HttpCodeConvention"/> sets their HTTP codes at run time.
	/// </remarks>
	/// <typeparam name="T"> The returned value type from the domain operation in <paramref name="domainResult"/> </typeparam>
	/// <param name="domainResult"> Details of the operation results (<see cref="DomainResult{T}"/>) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static Results<Ok<T>, NoContent, ProblemHttpResult> ToResult<T>(
		this IDomainResult<T> domainResult,
		Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
		=> ToOkOrNoContentResult(domainResult.Value, domainResult, errorAction);

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		For the minimal API only.
	///		The result is wrapped in a <see cref="Task"/>
	/// </summary>
	/// <remarks>
	///		The return type gives OpenAPI the 200 (OK) response with the value type, and the 204 (NoContent) response.
	///		Problem responses still need <c>.ProducesProblem()</c>, because <see cref="HttpCodeConvention"/> sets their HTTP codes at run time.
	/// </remarks>
	/// <typeparam name="T"> The returned value type from the domain operation in <paramref name="domainResultTask"/> </typeparam>
	/// <param name="domainResultTask"> Details of the operation results (<see cref="DomainResult{T}"/>) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<Results<Ok<T>, NoContent, ProblemHttpResult>> ToResult<T>(
		this Task<IDomainResult<T>> domainResultTask,
		Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
	{
		var domainResult = await domainResultTask;
		return ToOkOrNoContentResult(domainResult.Value, domainResult, errorAction);
	}

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		For the minimal API only.
	/// </summary>
	/// <remarks>
	///		The return type gives OpenAPI the 200 (OK) response with the value type, and the 204 (NoContent) response.
	///		Problem responses still need <c>.ProducesProblem()</c>, because <see cref="HttpCodeConvention"/> sets their HTTP codes at run time.
	/// </remarks>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static Results<Ok<V>, NoContent, ProblemHttpResult> ToResult<V, R>(
		this (V, R) domainResult,
		Action<ProblemDetails, R>? errorAction = null)
		where R : IDomainResult
	{
		var (value, res) = domainResult;
		return ToOkOrNoContentResult(value, res, errorAction);
	}

	/// <summary>
	///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
	///		For the minimal API only.
	///		The result is wrapped in a <see cref="Task{T}"/>
	/// </summary>
	/// <remarks>
	///		The return type gives OpenAPI the 200 (OK) response with the value type, and the 204 (NoContent) response.
	///		Problem responses still need <c>.ProducesProblem()</c>, because <see cref="HttpCodeConvention"/> sets their HTTP codes at run time.
	/// </remarks>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <param name="domainResultTask"> A task with returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<Results<Ok<V>, NoContent, ProblemHttpResult>> ToResult<V, R>(
		this Task<(V, R)> domainResultTask,
		Action<ProblemDetails, R>? errorAction = null)
		where R : IDomainResult
	{
		var (value, res) = await domainResultTask;
		return ToOkOrNoContentResult(value, res, errorAction);
	}

	/// <summary>
	///		Returns HTTP code 200 (OK) with the value, 204 (NoContent) for a <c>null</c> value, or a 4xx code in case of an error
	/// </summary>
	/// <remarks>
	///		A default value, such as 0 or false, is a value
	/// </remarks>
	private static Results<Ok<V>, NoContent, ProblemHttpResult> ToOkOrNoContentResult<V, R>(
		[AllowNull] V value,
		R errorDetails,
		Action<ProblemDetails, R>? errorAction)
		where R : IDomainResultBase
	{
		if (errorDetails.Status != DomainOperationStatus.Success)
			return ToProblemResult(errorDetails, errorAction);
		if (value is null)
			return TypedResults.NoContent();
		return TypedResults.Ok(value);
	}
}