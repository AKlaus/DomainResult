using System;
using System.Threading.Tasks;

using DomainResults.Common;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc
{
#if !NETCOREAPP2_0
	//
	// Conversion to HTTP code 204 (NoContent) - ActionResult<T> (the type exists starting from .NET Core 2.1 and not present in .NET Standard 2.0)
	//
	public static partial class DomainResultExtensions
	{
		/// <summary>
		///		Returns HTTP code 204 (NoContent) or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="T"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResult"> Details of the operation results </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static ActionResult<T> ToActionResultOfT<T>(this T domainResult,
														  Action<ProblemDetails, T>? errorAction = null)
														  where T : IDomainResult
			=> ToActionResult<object, T, NoContentResult>(null, domainResult, errorAction, (value) => new NoContentResult());

		/// <summary>
		///		Returns a task with HTTP code 204 (NoContent) or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="T"> The type derived from <see cref="IDomainResultBase"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResultTask"> A task with details of the operation results </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<ActionResult<T>> ToActionResultOfT<T>(this Task<T> domainResultTask,
																	   Action<ProblemDetails, T>? errorAction = null)
																	   where T : IDomainResult
			=> ToActionResult<object, T, NoContentResult>(null, await domainResultTask, errorAction, (value) => new NoContentResult());
	}
#endif
}