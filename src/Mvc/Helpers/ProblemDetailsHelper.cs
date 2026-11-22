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
	///		Creates a <typeparamref name="TValidationProblemDetails"/> for a result with member names in its errors, otherwise a <see cref="ProblemDetails"/> with the joined errors in 'detail'
	/// </summary>
	/// <remarks>
	///		As in the ASP.NET Core model validation response, 'errors' holds the messages keyed by member name, and 'detail' stays empty.
	///		A message without a member name has the key <see cref="string.Empty"/>. A message with several member names appears under each of them.
	/// </remarks>
	internal static ProblemDetails Create<TValidationProblemDetails>(int statusCode, string title, IDomainResultBase? errorDetails)
		where TValidationProblemDetails : HttpValidationProblemDetails, new()
	{
		var domainErrors = errorDetails?.DomainErrors;
		if (domainErrors?.Any(error => error.MemberNames.Count > 0) == true)
		{
			var validationProblemDetails = new TValidationProblemDetails { Title = title, Status = statusCode };
			foreach (var messages in from error in domainErrors
			                         from member in error.MemberNames.Count > 0 ? error.MemberNames : new[] { string.Empty }
			                         group error.Message by member)
				validationProblemDetails.Errors[messages.Key] = messages.ToArray();
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
