using System;
using System.Threading.Tasks;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc
{
#if !NETCOREAPP2_0
	//
	// Conversion to HTTP code 200 (OK) - ActionResult<T> (the type exists starting from .NET Core 2.1 and not present in earlier versions)
	//
	public static partial class DomainResultExtensions
	{
		/// <summary>
		///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="T"> The returned value type from the domain operation in <paramref name="domainResult"/> </typeparam>
		/// <param name="domainResult"> Details of the operation results (<see cref="DomainResult{T}"/>) </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static ActionResult<T> ToActionResultOfT<T>(this IDomainResult<T> domainResult,
														   Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
			=> ToActionResultOfT(domainResult.Value, domainResult, errorAction, (value) => new ActionResult<T>(value));

		/// <summary>
		///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
		///		The reesult is wrapped in a <see cref="Task{V}"/>
		/// </summary>
		/// <typeparam name="T"> The returned value type from the domain operation in <paramref name="domainResultTask"/> </typeparam>
		/// <param name="domainResultTask"> Details of the operation results (<see cref="DomainResult{T}"/>) </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<ActionResult<T>> ToActionResultOfT<T>(this Task<IDomainResult<T>> domainResultTask,
																	  Action<ProblemDetails, IDomainResult<T>>? errorAction = null)
		{
			var domainResult = await domainResultTask;
			return ToActionResultOfT(domainResult.Value, domainResult, errorAction, (value) => new ActionResult<T>(value));
		}

		/// <summary>
		///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResult"> Returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static ActionResult<V> ToActionResultOfT<V, R>(this (V, R) domainResult,
															  Action<ProblemDetails, R>? errorAction = null)
															  where R : IDomainResult
			=> ToActionResultOfT(domainResult.Item1, domainResult.Item2, errorAction, (value) => new ActionResult<V>(value));

		/// <summary>
		///		Returns HTTP code 200 (OK) with a value or a 4xx code in case of an error.
		///		The reesult is wrapped in a <see cref="Task{T}"/>
		/// </summary>
		/// <typeparam name="V"> The value type returned in a successful response </typeparam>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResultTask"> A task with returned value and details of the operation results (e.g. error messages) </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<ActionResult<V>> ToActionResultOfT<V, R>(this Task<(V, R)> domainResultTask,
																		 Action<ProblemDetails, R>? errorAction = null)
																		 where R : IDomainResult
		{
			var domainResult = await domainResultTask;
			return ToActionResultOfT(domainResult.Item1, domainResult.Item2, errorAction, (value) => new ActionResult<V>(value));
		}
	}
#endif
}