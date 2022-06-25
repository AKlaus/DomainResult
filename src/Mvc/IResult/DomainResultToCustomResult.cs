#if NET6_0_OR_GREATER
using System;
using System.Threading.Tasks;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace DomainResults.Mvc;

public static partial class DomainResultExtensions
{
	//
	// Conversion to custom HTTP codes (e.g. 201 Created) and objects (e.g. VirtualFileResult, FileStreamResult, etc.) - IResult (the minimal API)
	//

	/// <summary>
	///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
	/// </summary>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <typeparam name="TResult"> The result type returned in <paramref name="valueToResultFunc"/> if the domain operation was successful </typeparam>
	/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="valueToResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static IResult ToCustomResult<V, R, TResult>(this (V, R) domainResult,
	                                                    ValueToResultFunc<V, TResult> valueToResultFunc,
	                                                    Action<ProblemDetails, R>? errorAction = null)
													where R : IDomainResultBase
													where TResult : IResult
		=> ToResult(domainResult.Item1, domainResult.Item2, errorAction, valueToResultFunc);

	/// <summary>
	///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
	/// </summary>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <typeparam name="TResult"> The result type returned in <paramref name="valueToResultFunc"/> if the domain operation was successful </typeparam>
	/// <param name="domainResultTask"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="valueToResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<IResult> ToCustomResult<V, R, TResult>(this Task<(V, R)> domainResultTask,
													ValueToResultFunc<V, TResult> valueToResultFunc,
													Action<ProblemDetails, R>? errorAction = null)
													where R : IDomainResultBase
													where TResult : IResult
	{
		var domainResult = await domainResultTask;
		return ToResult(domainResult.Item1, domainResult.Item2, errorAction, valueToResultFunc); 
	}

	/// <summary>
	///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
	/// </summary>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="TResult"> The result type returned in <paramref name="valueToResultFunc"/> if the domain operation was successful </typeparam>
	/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="valueToResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static IResult ToCustomResult<V, TResult>(this IDomainResult<V> domainResult,
													ValueToResultFunc<V, TResult> valueToResultFunc,
													Action<ProblemDetails, IDomainResult<V>>? errorAction = null)
													where TResult : IResult
		=> ToResult(domainResult.Value, domainResult, errorAction, valueToResultFunc);

	/// <summary>
	///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
	/// </summary>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="TResult"> The result type returned in <paramref name="valueToResultFunc"/> if the domain operation was successful </typeparam>
	/// <param name="domainResultTask"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="valueToResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<IResult> ToCustomResult<V, TResult>(this Task<IDomainResult<V>> domainResultTask,
													ValueToResultFunc<V, TResult> valueToResultFunc,
													Action<ProblemDetails, IDomainResult<V>>? errorAction = null)
													where TResult : IResult
	{
		var domainResult = await domainResultTask;
		return ToResult(domainResult.Value, domainResult, errorAction, valueToResultFunc); 
	}
}
#endif