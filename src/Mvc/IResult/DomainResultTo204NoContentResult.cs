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
	// Conversion to HTTP code 204 (NoContent) - IResult (the minimal API)
	//

	/// <summary>
	///		Returns HTTP code 204 (NoContent) or a 4xx code in case of an error
	///		For the minimal API only.
	/// </summary>
	/// <remarks>
	///		The return type gives OpenAPI the 204 (NoContent) response.
	///		Problem responses still need <c>.ProducesProblem()</c>, because <see cref="HttpCodeConvention"/> sets their HTTP codes at run time.
	/// </remarks>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <param name="domainResult"> Details of the operation results </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static Results<NoContent, ProblemHttpResult> ToResult<R>(
		this R domainResult,
		Action<ProblemDetails, R>? errorAction = null)
		where R : IDomainResult
		=> ToResult<object, R, NoContent>(null, domainResult, errorAction, _ => TypedResults.NoContent());

	/// <summary>
	///		Returns a task with HTTP code 204 (NoContent) or a 4xx code in case of an error
	///		For the minimal API only.
	/// </summary>
	/// <remarks>
	///		The return type gives OpenAPI the 204 (NoContent) response.
	///		Problem responses still need <c>.ProducesProblem()</c>, because <see cref="HttpCodeConvention"/> sets their HTTP codes at run time.
	/// </remarks>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResultBase"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <param name="domainResultTask"> A task with details of the operation results </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<Results<NoContent, ProblemHttpResult>> ToResult<R>(
		this Task<R> domainResultTask,
		Action<ProblemDetails, R>? errorAction = null)
		where R : IDomainResult
		=> ToResult<object, R, NoContent>(null, await domainResultTask, errorAction, _ => TypedResults.NoContent());
}