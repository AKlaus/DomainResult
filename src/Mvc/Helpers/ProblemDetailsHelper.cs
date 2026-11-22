using DomainResults.Common;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Mvc.Helpers;

/// <summary>
///		Builds the problem details of a failed result for the <c>IActionResult</c> and the <c>IResult</c> conversions
/// </summary>
internal static class ProblemDetailsHelper
{
	/// <summary>
	///		Creates a <typeparamref name="TValidationProblemDetails"/> for a result with validation errors, otherwise a <see cref="ProblemDetails"/> with the joined errors in 'detail'
	/// </summary>
	/// <remarks>
	///		A <typeparamref name="TValidationProblemDetails"/> has no 'detail', as in the ASP.NET Core model validation response, because 'errors' holds every message.
	/// </remarks>
	internal static ProblemDetails Create<TValidationProblemDetails>(int statusCode, string title, IDomainResultBase? errorDetails)
		where TValidationProblemDetails : HttpValidationProblemDetails, new()
	{
		if (errorDetails?.ValidationErrors.Count > 0)
		{
			var validationProblemDetails = new TValidationProblemDetails { Title = title, Status = statusCode };
			foreach (var (member, messages) in errorDetails.ValidationErrors)
				validationProblemDetails.Errors[member] = messages.ToArray();
			return validationProblemDetails;
		}
		return new ProblemDetails
			{
				Title = title,
				Detail = errorDetails?.Errors.Any() == true ? string.Join(", ", errorDetails.Errors) : null,
				Status = statusCode
			};
	}
}
