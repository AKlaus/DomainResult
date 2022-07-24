using System;
using System.Threading.Tasks;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace DomainResults.Mvc;

//
// Conversion to custom HTTP codes - ActionResult<T> (the type exists starting from .NET Core 2.1 and not present in .NET Standard 2.0)
//
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
	/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="valueToActionResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static ActionResult<V> ToCustomActionResultOfT<V, R>(this (V, R) domainResult,
																Func<V, ActionResult<V>> valueToActionResultFunc,
																Action<ProblemDetails, R>? errorAction = null)
																where R : IDomainResultBase
		=> ToActionResultOfT(domainResult.Item1, domainResult.Item2, errorAction, valueToActionResultFunc);

	/// <summary>
	///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
	/// </summary>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
	/// <param name="domainResultTask"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="valueToActionResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<ActionResult<V>> ToCustomActionResultOfT<V, R>(this Task<(V, R)> domainResultTask,
																			Func<V, ActionResult<V>> valueToActionResultFunc,
																			Action<ProblemDetails, R>? errorAction = null)
																			where R : IDomainResultBase
	{
		var domainResult = await domainResultTask;
		return ToActionResultOfT(domainResult.Item1, domainResult.Item2, errorAction, valueToActionResultFunc); 
	}

	/// <summary>
	///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
	/// </summary>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="valueToActionResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static ActionResult<V> ToCustomActionResultOfT<V>(this IDomainResult<V> domainResult,
															 Func<V, ActionResult<V>> valueToActionResultFunc,
															 Action<ProblemDetails, IDomainResult<V>>? errorAction = null)
		=> ToActionResultOfT(domainResult.Value, domainResult, errorAction, valueToActionResultFunc);

	/// <summary>
	///		Custom conversion of successful and unsuccessful domain results to specified <see cref="ActionResult"/> types
	/// </summary>
	/// <typeparam name="V"> The value type returned in a successful response </typeparam>
	/// <param name="domainResultTask"> Returned value and details of the operation results (e.g. error messages) </param>
	/// <param name="valueToActionResultFunc"> The custom function for converting a value to <see cref="ActionResult"/> type. </param>
	/// <param name="errorAction"> Optional processing in case of an error </param>
	public static async Task<ActionResult<V>> ToCustomActionResultOfT<V>(this Task<IDomainResult<V>> domainResultTask,
																		 Func<V, ActionResult<V>> valueToActionResultFunc,
																		 Action<ProblemDetails, IDomainResult<V>>? errorAction = null)
	{
		var domainResult = await domainResultTask;
		return ToActionResultOfT(domainResult.Value, domainResult, errorAction, valueToActionResultFunc); 
	}
}