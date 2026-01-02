using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Tests.Mvc;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
[Collection("Sequential")]
public class ActionResult_Conventions_Tests
{
	[Theory]
	[InlineData(null, 400)]
	[InlineData(422,  422)]
	public void FailedHttpCode_Is_Honoured_in_Failed_Response_Test(int? failedHttpCode, int expectedFailedHttpCode)
	{
		var defaultValue = HttpCodeConvention.FailedHttpCode;

		// GIVEN a custom HTTP code for errors
		if (failedHttpCode.HasValue)
			HttpCodeConvention.FailedHttpCode = failedHttpCode.Value;

		// WHEN a IDomainResult with Status 'Error' gets converted
		var domainResult = IDomainResult.Failed();
		//	 to ActionResult
		var actionRes = domainResult.ToActionResult() as ObjectResult;
		//	 to IResult (minimal API)
		var res = domainResult.ToResult();

		// THEN the HTTP code is expected
		//		for the ActionResult conversion 
		Assert.Equal(expectedFailedHttpCode, actionRes!.StatusCode);
		Assert.Equal(expectedFailedHttpCode, (actionRes.Value as ProblemDetails)!.Status);
		//		for the IResult (minimal API) conversion
		res.AssertObjectResultTypeWithProblemDetails(expectedFailedHttpCode);

		HttpCodeConvention.FailedHttpCode = defaultValue;
	}

	[Theory]
	[InlineData(null, "Bad Request")]
	[InlineData("1", "1")]
	public void FailedProblemDetailsTitle_Is_Honoured_in_Error_Response_Test(string? failedTitle, string expectedFailedTitle)
	{
		var defaultValue = HttpCodeConvention.FailedProblemDetailsTitle;

		// GIVEN a custom HTTP code for errors
		if (!string.IsNullOrEmpty(failedTitle))
			HttpCodeConvention.FailedProblemDetailsTitle = failedTitle;

		// WHEN a IDomainResult with Status 'Error' gets converted
		//	 to ActionResult
		var domainResult = IDomainResult.Failed();
		var actionRes = domainResult.ToActionResult() as ObjectResult;
		//	 to IResult (minimal API)
		var res = domainResult.ToResult();

		// THEN the ProblemDetails Title is expected
		//		for the ActionResult conversion 
		var problemDetails = actionRes!.Value as ProblemDetails;
		Assert.Equal(expectedFailedTitle, problemDetails!.Title);
		//		for the IResult (minimal API) conversion
		res.AssertObjectResultTypeWithProblemDetails(HttpCodeConvention.FailedHttpCode, expectedFailedTitle);

		HttpCodeConvention.FailedProblemDetailsTitle = defaultValue;
	}

	[Theory]
	[InlineData(null, 404)]
	[InlineData(499, 499)]
	public void NotFoundHttpCode_Is_Honoured_in_NotFound_Response_Test(int? notFoundHttpCode, int expectedNotFoundHttpCode)
	{
		var defaultValue = HttpCodeConvention.NotFoundHttpCode;

		// GIVEN a custom HTTP code for 'Not Found'
		if (notFoundHttpCode.HasValue)
			HttpCodeConvention.NotFoundHttpCode = notFoundHttpCode.Value;

		// WHEN a IDomainResult with Status 'Not Found' gets converted
		var domainResult = IDomainResult.NotFound();
		//	 to ActionResult
		var actionRes = domainResult.ToActionResult() as ObjectResult;
		//	 to IResult (minimal API)
		var res = domainResult.ToResult();

		// THEN the HTTP code is expected
		//		for the ActionResult conversion 
		Assert.Equal(expectedNotFoundHttpCode, actionRes!.StatusCode);
		Assert.Equal(expectedNotFoundHttpCode, (actionRes.Value as ProblemDetails)!.Status);
		//		for the IResult (minimal API) conversion
		res.AssertObjectResultTypeWithProblemDetails(expectedNotFoundHttpCode);

		HttpCodeConvention.NotFoundHttpCode = defaultValue;
	}

	[Theory]
	[InlineData(null, "Not Found")]
	[InlineData("1", "1")]
	public void NotFoundHttpDetailsTitle_Is_Honoured_in_NotFound_Response_Test(string? notFoundTitle, string expectedNotFoundTitle)
	{
		var defaultValue = HttpCodeConvention.NotFoundProblemDetailsTitle;

		// GIVEN a custom HTTP code for errors
		if (!string.IsNullOrEmpty(notFoundTitle))
			HttpCodeConvention.NotFoundProblemDetailsTitle = notFoundTitle;

		// WHEN a IDomainResult with Status 'Not Found' gets converted
		var domainResult = IDomainResult.NotFound();
		//	 to ActionResult
		var actionRes = domainResult.ToActionResult() as ObjectResult;
		//	 to IResult (minimal API)
		var res = domainResult.ToResult();

		// THEN the ProblemDetails Title is expected
		var problemDetails = actionRes!.Value as ProblemDetails;
		Assert.Equal(expectedNotFoundTitle, problemDetails!.Title);
		//		for the IResult (minimal API) conversion
		res.AssertObjectResultTypeWithProblemDetails(HttpCodeConvention.NotFoundHttpCode, expectedNotFoundTitle);

		HttpCodeConvention.NotFoundProblemDetailsTitle = defaultValue;
	}
}