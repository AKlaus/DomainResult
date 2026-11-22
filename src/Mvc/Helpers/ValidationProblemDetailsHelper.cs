using Microsoft.AspNetCore.Http;

namespace DomainResults.Mvc.Helpers;

/// <summary>
///		Builds the problem details of a result with validation errors for the <c>IActionResult</c> and the <c>IResult</c> conversions
/// </summary>
internal static class ValidationProblemDetailsHelper
{
	/// <summary>
	///		Creates a <typeparamref name="T"/> with the validation error messages keyed by member name
	/// </summary>
	/// <remarks>
	///		The 'detail' stays empty, as in the ASP.NET Core model validation response, because 'errors' holds every message.
	/// </remarks>
	internal static T Create<T>(int statusCode, string title, IReadOnlyDictionary<string, string[]> validationErrors) where T : HttpValidationProblemDetails, new()
	{
		var problemDetails = new T { Title = title, Status = statusCode };
		foreach (var (member, messages) in validationErrors)
			problemDetails.Errors[member] = messages.ToArray();
		return problemDetails;
	}
}
