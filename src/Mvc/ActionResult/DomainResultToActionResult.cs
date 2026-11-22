using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Mvc.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DomainResults.Mvc;

/// <summary>
///     Converts domain response to <see cref="ActionResult" /> or <see cref="IResult" /> response for the WebAPI
/// </summary>
public static partial class DomainResultExtensions
{
	/// <summary>
	///		Delegate for a function to convert a value to <see cref="IActionResult"/>.
	/// </summary>
	/// <remarks>
	///		It could have been replaced with 'Func{V, ActionResult}', but there is no way to enforce [AllowNull] on the generic Func declaration
	/// </remarks>
	/// <typeparam name="V"> The returned value type (either a struct or a class) </typeparam>
	/// <typeparam name="T"> The returned action result type of <see cref="IActionResult"/> </typeparam>
	/// <param name="value"> The value to be returned </param>
	/// <returns> Action result </returns>
	public delegate T ValueToActionResultFunc<V, out T>([AllowNull] V value) where T : IActionResult;

	private static ActionResult ToActionResult<V, R, TResult>([AllowNull] V value,
	                                                          R errorDetails,
	                                                          Action<ProblemDetails, R>? errorAction,
	                                                          ValueToActionResultFunc<V, TResult> valueToActionResultFunc)
																where R : IDomainResultBase
																where TResult : ActionResult
		=> errorDetails.Status switch
		{
			DomainOperationStatus.NotFound		 => CreateProblemResponse(HttpCodeConvention.NotFoundHttpCode,	 	 HttpCodeConvention.NotFoundProblemDetailsTitle,		errorDetails, errorAction),
			DomainOperationStatus.Unauthorized	 => CreateProblemResponse(HttpCodeConvention.UnauthorizedHttpCode,	 HttpCodeConvention.UnauthorizedProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Conflict		 => CreateProblemResponse(HttpCodeConvention.ConflictHttpCode,	 	 HttpCodeConvention.ConflictProblemDetailsTitle,		errorDetails, errorAction),
			DomainOperationStatus.ContentTooLarge=> CreateProblemResponse(HttpCodeConvention.ContentTooLargeHttpCode,HttpCodeConvention.ContentTooLargeProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Failed		 => CreateProblemResponse(HttpCodeConvention.FailedHttpCode,	 	 HttpCodeConvention.FailedProblemDetailsTitle,			errorDetails, errorAction),
			DomainOperationStatus.CriticalDependencyError
												 => CreateProblemResponse(HttpCodeConvention.CriticalDependencyErrorHttpCode,	HttpCodeConvention.CriticalDependencyErrorProblemDetailsTitle,	errorDetails, errorAction),
			DomainOperationStatus.Success		 => valueToActionResultFunc(value),
			_ => throw new ArgumentOutOfRangeException(),
		};

	/// <summary>
	///		Creates a ProblemDetails response with a 4xx status for error scenarios based on RFC 7807.
	/// </summary>
	private static ObjectResult CreateProblemResponse<R>(int statusCode,
	                                                     string title,
	                                                     R errorDetails,
	                                                     Action<ProblemDetails, R>? errorAction = null) where R : IDomainResultBase
	{
		var problemDetails = ProblemDetailsHelper.Create<ValidationProblemDetails>(statusCode, title, errorDetails);

		errorAction?.Invoke(problemDetails, errorDetails!);

		return new ProblemDetailsObjectResult(problemDetails) { StatusCode = problemDetails.Status };
	}

	/// <summary>
	///		An <see cref="ObjectResult"/> that completes its <see cref="ProblemDetails"/> with the <see cref="ProblemDetailsFactory"/> when it runs.
	/// </summary>
	/// <remarks>
	///		The response then matches <c>ControllerBase.Problem()</c>: the factory adds 'type' and 'traceId', and applies the <c>AddProblemDetails()</c> customisation.
	///		The factory gets the fields that the 'errorAction' set, so it keeps them. Extensions that the factory also sets keep the factory value.
	///		The customisation runs before the 'errorAction' extensions join the body, so it cannot read them. From .NET 8, the <c>IResult</c> conversion passes them to the customisation.
	///		A <see cref="ValidationProblemDetails"/> stays one, and keeps its 'errors'. The customisation runs before the 'errors' join the body, so it cannot read them.
	///		Without a registered factory, the response keeps the <see cref="ProblemDetails"/> as it is.
	/// </remarks>
	private sealed class ProblemDetailsObjectResult(ProblemDetails problemDetails) : ObjectResult(problemDetails)
	{
		public override Task ExecuteResultAsync(ActionContext context)
		{
			var factory = context.HttpContext.RequestServices.GetService<ProblemDetailsFactory>();
			if (factory is not null && Value is ProblemDetails source)
			{
				var target = source is ValidationProblemDetails validationSource
					? CreateValidationProblemDetails(factory, context.HttpContext, validationSource)
					: factory.CreateProblemDetails(context.HttpContext, source.Status ?? StatusCode, source.Title, source.Type, source.Detail, source.Instance);
				foreach (var (key, value) in source.Extensions)
					target.Extensions.TryAdd(key, value);

				Value = target;
				StatusCode = target.Status;
			}
			return base.ExecuteResultAsync(context);
		}

		private ValidationProblemDetails CreateValidationProblemDetails(ProblemDetailsFactory factory, HttpContext httpContext, ValidationProblemDetails source)
		{
			// An empty model state keeps the messages and the member names as they are. The model state would replace an empty message and merge member names that differ in case.
			var target = factory.CreateValidationProblemDetails(httpContext, new ModelStateDictionary(), source.Status ?? StatusCode, source.Title, source.Type, source.Detail, source.Instance);
			foreach (var (member, messages) in source.Errors)
				target.Errors[member] = messages;
			return target;
		}
	}
}