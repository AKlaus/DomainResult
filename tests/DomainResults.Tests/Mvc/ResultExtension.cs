using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Tests.Mvc;

/// <summary>
///		Helper methods to validate <see cref="IResult"/> properties
/// </summary>
public static class ResultExtension
{
	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'ObjectResult' class with a 'ProblemDetails'
	/// </summary>
	public static void AssertObjectResultTypeWithProblemDetails(this IResult res, int expectedHttpStatus, string? expectedTitle = null, string? expectedDetail = null)
	{
		// Assert on the status code
		Assert.Equal(expectedHttpStatus, (res as IStatusCodeHttpResult)?.StatusCode);
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.Equal(expectedHttpStatus, problemDetails?.Status);

		// Assert on the title if provided
		if (!string.IsNullOrEmpty(expectedTitle))
			Assert.Equal(expectedTitle, problemDetails?.Title);
		// Assert on the error details if provided
		if (!string.IsNullOrEmpty(expectedDetail))
			Assert.Equal(expectedDetail, problemDetails?.Detail);
	}

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'OkObjectResult' class with correct value
	/// </summary>
	public static void AssertOkObjectResultTypeAndValue<TValue>(this IResult res, TValue expectedValue)
	{
		// Assert on 200 OK response type
		var resTyped = res as Microsoft.AspNetCore.Http.HttpResults.Ok<TValue>;
		Assert.NotNull(resTyped);
		Assert.Equal(200, resTyped!.StatusCode);

		// Assert on the expected value
		Assert.Equal(expectedValue, resTyped.Value);
	}

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'NoContentResult' class
	/// </summary>
	public static void AssertNoContentResultType(this IResult res)
	{
		var resTyped = res as Microsoft.AspNetCore.Http.HttpResults.NoContent;
		Assert.NotNull(resTyped);
		Assert.Equal(204, resTyped!.StatusCode);
	}

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'CreatedResult' class
	/// </summary>
	public static void AssertCreatedResultTypeAndValueAndLocation<TValue>(this IResult res, TValue expectedValue, string expectedLocation)
	{
		// Assert on 201 Created response type
		var resTyped = res as Microsoft.AspNetCore.Http.HttpResults.Created<TValue>;
		Assert.NotNull(resTyped);
		Assert.Equal(201, resTyped!.StatusCode);

		// Assert on the expected value
		Assert.Equal(expectedValue, resTyped.Value);
		// Assert on the expected location
		Assert.Equal(expectedLocation, resTyped.Location);
	}
}

