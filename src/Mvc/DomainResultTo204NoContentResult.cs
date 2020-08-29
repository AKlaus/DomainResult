﻿using System;
using System.Threading.Tasks;

using DomainResults.Domain;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc
{
	public static partial class DomainResultExtensions
	{
		//
		// Conversion to HTTP code 204 (NoContent)
		//

		/// <summary>
		///		Returns HTTP code 204 (NoContent) or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResult"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResult"> Details of the operation results </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static ActionResult ToActionResult<R>(this R domainResult,
													 Action<ProblemDetails, R>? errorAction = null)
													 where R : IDomainResult
			=> ToActionResult<object, R, NoContentResult>(null, domainResult, errorAction, (value) => new NoContentResult());

		/// <summary>
		///		Returns a task with HTTP code 204 (NoContent) or a 4xx code in case of an error
		/// </summary>
		/// <typeparam name="R"> The type derived from <see cref="IDomainResultBase"/>, e.g. <see cref="DomainResult"/> </typeparam>
		/// <param name="domainResultTask"> A task with details of the operation results </param>
		/// <param name="errorAction"> Optional processing in case of an error </param>
		public static async Task<IActionResult> ToActionResult<R>(this Task<R> domainResultTask,
																  Action<ProblemDetails, R>? errorAction = null)
																  where R : IDomainResult
			=> ToActionResult<object, R, NoContentResult>(null, await domainResultTask, errorAction, (value) => new NoContentResult());
	}
}