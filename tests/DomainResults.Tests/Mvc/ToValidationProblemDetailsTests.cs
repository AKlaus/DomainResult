using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using Xunit;

namespace DomainResults.Tests.Mvc;

[Collection("Sequential")]
public class ToValidationProblemDetailsTests
{
	[Theory]
	[MemberData(nameof(ActionResultCases))]
	public async Task ToActionResult_ValidationErrors_ReturnsValidationProblemDetails(Func<IEnumerable<ValidationResult>, IActionResult> toActionResult)
	{
		// GIVEN a failed result with validation errors of two members
		// WHEN the IActionResult conversion runs
		var actionResult = toActionResult(ValidationResults);

		// THEN the value is a ValidationProblemDetails with the errors keyed by member, and no 'detail'
		var problemDetails = Assert.IsType<ValidationProblemDetails>(Assert.IsAssignableFrom<ObjectResult>(actionResult).Value);
		Assert.Equal(HttpCodeConvention.FailedHttpCode, problemDetails.Status);
		Assert.Equal(HttpCodeConvention.FailedProblemDetailsTitle, problemDetails.Title);
		Assert.Null(problemDetails.Detail);
		Assert.Equal(ExpectedErrors, problemDetails.Errors);

		// and the response body has the same errors, the 'type' and the 'traceId' of the ProblemDetailsFactory
		var (statusCode, body) = await actionResult.ExecuteToJsonAsync();
		Then_Body_Has_Validation_Errors(statusCode, body);
		Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
	}
	public static TheoryData<Func<IEnumerable<ValidationResult>, IActionResult>> ActionResultCases => new()
		{
			v => DomainResult.Failed(v).ToActionResult(),
			v => DomainResult.Failed<int>(v).ToActionResult(),
			v => ((IConvertToActionResult)DomainResult.Failed<int>(v).ToActionResultOfT()).Convert(),
			v => IDomainResult.Failed<int>(v).ToActionResult()
		};

	[Theory]
	[MemberData(nameof(ResultCases))]
	public async Task ToResult_ValidationErrors_ReturnsHttpValidationProblemDetails(Func<IEnumerable<ValidationResult>, IResult> toResult)
	{
		// GIVEN a failed result with validation errors of two members
		// WHEN the IResult conversion runs
		var result = toResult(ValidationResults);

		// THEN the value is an HttpValidationProblemDetails with the errors keyed by member, and no 'detail'
		var problemDetails = Assert.IsType<HttpValidationProblemDetails>(Assert.IsType<ProblemHttpResult>(result.GetInnerResult()).ProblemDetails);
		Assert.Equal(HttpCodeConvention.FailedHttpCode, problemDetails.Status);
		Assert.Equal(HttpCodeConvention.FailedProblemDetailsTitle, problemDetails.Title);
		Assert.Null(problemDetails.Detail);
		Assert.Equal(ExpectedErrors, problemDetails.Errors);

		// and the response body has the same errors and the 'type'
		var (statusCode, body) = await result.ExecuteToJsonAsync();
		Then_Body_Has_Validation_Errors(statusCode, body);
	}
	public static TheoryData<Func<IEnumerable<ValidationResult>, IResult>> ResultCases => new()
		{
			v => DomainResult.Failed(v).ToResult(),
			v => DomainResult.Failed<int>(v).ToResult(),
			v => IDomainResult.Failed<int>(v).ToResult()
		};

	[Fact]
	public async Task ToActionResult_EmptyMessageAndMembersThatDifferInCase_KeepsErrorsAsTheyAre()
	{
		// GIVEN validation errors with an empty message, and two members that differ only in case
		var domainResult = DomainResult.Failed([new ValidationResult("", ["name"]), new ValidationResult("2", ["Name"])]);

		// WHEN the IActionResult conversion runs with the ProblemDetailsFactory
		var (_, body) = await domainResult.ToActionResult().ExecuteToJsonAsync();

		// THEN the body keeps the empty message and both members
		Assert.Equal(new Dictionary<string, string[]> { ["name"] = [""], ["Name"] = ["2"] }, ReadErrors(body));
	}

	[Fact]
	public async Task ToActionResult_ErrorActionAddsError_WritesAddedError()
	{
		// GIVEN an 'errorAction' that adds a validation error
		var domainResult = DomainResult.Failed(ValidationResults);
		void AddError(ProblemDetails problemDetails, IDomainResult _) => ((ValidationProblemDetails)problemDetails).Errors["extra"] = ["3"];

		// WHEN the IActionResult conversion runs with the ProblemDetailsFactory
		var (_, body) = await domainResult.ToActionResult(AddError).ExecuteToJsonAsync();

		// THEN the body has the added error next to the validation errors
		Assert.Equal(new Dictionary<string, string[]>(ExpectedErrors) { ["extra"] = ["3"] }, ReadErrors(body));
	}

	#region Auxiliary methods [PRIVATE] -----------------------------------

	private static readonly ValidationResult[] ValidationResults = [new("1", ["fieldName1"]), new("2", ["fieldName2"])];
	private static readonly Dictionary<string, string[]> ExpectedErrors = new() { ["fieldName1"] = ["1"], ["fieldName2"] = ["2"] };

	private static void Then_Body_Has_Validation_Errors(int statusCode, JsonElement body)
	{
		Assert.Equal(HttpCodeConvention.FailedHttpCode, statusCode);
		Assert.Equal(HttpCodeConvention.FailedHttpCode, body.GetProperty("status").GetInt32());
		Assert.Equal(HttpCodeConvention.FailedProblemDetailsTitle, body.GetProperty("title").GetString());
		Assert.Equal(TypedResults.Problem(statusCode: statusCode).ProblemDetails.Type, body.GetProperty("type").GetString());
		Assert.False(body.TryGetProperty("detail", out _));
		Assert.Equal(ExpectedErrors, ReadErrors(body));
	}

	private static Dictionary<string, string[]>? ReadErrors(JsonElement body)
		=> body.GetProperty("errors").Deserialize<Dictionary<string, string[]>>();

	#endregion // Auxiliary methods [PRIVATE] -----------------------------
}
