using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Tests.Mvc
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[Collection("Sequential")]
	public class ActionResult_Conventions_Tests
	{
		[Theory]
		[InlineData(null, 400)]
		[InlineData(422,  422)]
		public void ErrorHttpCode_Is_Honoured_in_Error_Response_Test(int? errorHttpCode, int expectedErrorHttpCode)
		{
			var defaultValue = ActionResultConventions.ErrorHttpCode;

			// GIVEN a custom HTTP code for errors
			if (errorHttpCode.HasValue)
				ActionResultConventions.ErrorHttpCode = errorHttpCode.Value;

			// WHEN a IDomainResult with Status 'Error' gets converted to ActionResult
			var domainResult = IDomainResult.Failed();
			var actionRes = domainResult.ToActionResult() as ObjectResult;

			// THEN the HTTP code is expected
			Assert.Equal(expectedErrorHttpCode, actionRes!.StatusCode);

			ActionResultConventions.ErrorHttpCode = defaultValue;
		}

		[Theory]
		[InlineData(null, "Bad Request")]
		[InlineData("1", "1")]
		public void ErrorProblemDetailsTitle_Is_Honoured_in_Error_Response_Test(string errorTitle, string expectedErrorTitle)
		{
			var defaultValue = ActionResultConventions.ErrorProblemDetailsTitle;

			// GIVEN a custom HTTP code for errors
			if (!string.IsNullOrEmpty(errorTitle))
				ActionResultConventions.ErrorProblemDetailsTitle = errorTitle;

			// WHEN a IDomainResult with Status 'Error' gets converted to ActionResult
			var domainResult = IDomainResult.Failed();
			var actionRes = domainResult.ToActionResult() as ObjectResult;
			var problemDetails = actionRes!.Value as ProblemDetails;

			// THEN the ProblemDetails Title is expected
			Assert.Equal(expectedErrorTitle, problemDetails!.Title);

			ActionResultConventions.ErrorProblemDetailsTitle = defaultValue;
		}

		[Theory]
		[InlineData(null, 404)]
		[InlineData(499, 499)]
		public void NotFoundHttpCode_Is_Honoured_in_NotFound_Response_Test(int? notFoundHttpCode, int expectedNotFoundHttpCode)
		{
			var defaultValue = ActionResultConventions.NotFoundHttpCode;

			// GIVEN a custom HTTP code for 'Not Found'
			if (notFoundHttpCode.HasValue)
				ActionResultConventions.NotFoundHttpCode = notFoundHttpCode.Value;

			// WHEN a IDomainResult with Status 'Not Found' gets converted to ActionResult
			var domainResult = IDomainResult.NotFound();
			var actionRes = domainResult.ToActionResult() as ObjectResult;

			// THEN the HTTP code is expected
			Assert.Equal(expectedNotFoundHttpCode, actionRes!.StatusCode);

			ActionResultConventions.NotFoundHttpCode = defaultValue;
		}

		[Theory]
		[InlineData(null, "Not Found")]
		[InlineData("1", "1")]
		public void NotFoundHttpDetailsTitle_Is_Honoured_in_NotFound_Response_Test(string notFoundTitle, string expectedNotFoundTitle)
		{
			var defaultValue = ActionResultConventions.NotFoundProblemDetailsTitle;

			// GIVEN a custom HTTP code for errors
			if (!string.IsNullOrEmpty(notFoundTitle))
				ActionResultConventions.NotFoundProblemDetailsTitle = notFoundTitle;

			// WHEN a IDomainResult with Status 'Not Found' gets converted to ActionResult
			var domainResult = IDomainResult.NotFound();
			var actionRes = domainResult.ToActionResult() as ObjectResult;
			var problemDetails = actionRes!.Value as ProblemDetails;

			// THEN the ProblemDetails Title is expected
			Assert.Equal(expectedNotFoundTitle, problemDetails!.Title);

			ActionResultConventions.NotFoundProblemDetailsTitle = defaultValue;
		}
	}
}
