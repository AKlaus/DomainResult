#if NET6_0_OR_GREATER
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
#if NET6_0
		Assert.Equal("Microsoft.AspNetCore.Http.Result.ObjectResult", res.GetType().FullName);
		// NOTE that we don't check Microsoft.AspNetCore.Http.Result.ObjectResult.StatusCode property as it always remains NULL.
		Assert.Equal(expectedHttpStatus, res.GetProblemDetails()!.Status);
#elif NET7_0_OR_GREATER
		Assert.Equal(expectedHttpStatus, (res as IStatusCodeHttpResult)?.StatusCode);
		var problemDetails = (res as IValueHttpResult)?.Value as ProblemDetails;
		Assert.Equal(expectedHttpStatus, problemDetails?.Status);
#endif
		// Assert on the title if provided
		if (!string.IsNullOrEmpty(expectedTitle))
#if NET6_0
			Assert.Equal(expectedTitle, res.GetProblemDetails()!.Title);
#elif NET7_0_OR_GREATER
			Assert.Equal(expectedTitle, problemDetails?.Title);
#endif
		// Assert on the error details if provided
		if (!string.IsNullOrEmpty(expectedDetail))
#if NET6_0
			Assert.Equal(expectedDetail, res.GetProblemDetails()!.Detail);
#elif NET7_0_OR_GREATER
			Assert.Equal(expectedDetail, problemDetails?.Detail);
#endif
	}

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'OkObjectResult' class with correct value
	/// </summary>
	public static void AssertOkObjectResultTypeAndValue<TValue>(this IResult res, TValue expectedValue)
	{
		// Assert on 200 OK response type
#if NET6_0
		Assert.Equal("Microsoft.AspNetCore.Http.Result.OkObjectResult", res.GetType().FullName);
		Assert.Equal(200, res.GetPropValue("StatusCode"));
#elif NET7_0_OR_GREATER
		var resTyped = res as Microsoft.AspNetCore.Http.HttpResults.Ok<TValue>;
		Assert.NotNull(resTyped);
		Assert.Equal(200, resTyped!.StatusCode);
#endif
		// Assert on the expected value
#if NET6_0
		Assert.Equal(expectedValue, res.GetPropValue());
#elif NET7_0_OR_GREATER
		Assert.Equal(expectedValue, resTyped.Value);
#endif
	}

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'NoContentResult' class
	/// </summary>
	public static void AssertNoContentResultType(this IResult res)
	{
#if NET6_0
		Assert.Equal("Microsoft.AspNetCore.Http.Result.NoContentResult", res.GetType().FullName);
		Assert.Equal(204, res.GetPropValue("StatusCode"));
#elif NET7_0_OR_GREATER
		var resTyped = res as Microsoft.AspNetCore.Http.HttpResults.NoContent;
		Assert.NotNull(resTyped);
		Assert.Equal(204, resTyped!.StatusCode);
#endif
	}

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'CreatedResult' class
	/// </summary>
	public static void AssertCreatedResultTypeAndValueAndLocation<TValue>(this IResult res, TValue expectedValue, string expectedLocation)
	{
		// Assert on 201 Created response type
#if NET6_0
		Assert.Equal("Microsoft.AspNetCore.Http.Result.CreatedResult", res.GetType().FullName);
		Assert.Equal(201, res.GetPropValue("StatusCode"));
#elif NET7_0_OR_GREATER
		var resTyped = res as Microsoft.AspNetCore.Http.HttpResults.Created<TValue>;
		Assert.NotNull(resTyped);
		Assert.Equal(201, resTyped!.StatusCode);
#endif
		// Assert on the expected value
#if NET6_0
		Assert.Equal(expectedValue, res.GetPropValue());
#elif NET7_0_OR_GREATER
		Assert.Equal(expectedValue, resTyped.Value);
#endif
		// Assert on the expected location
#if NET6_0
		Assert.Equal(expectedLocation, res.GetPropValue("Location"));
#elif NET7_0_OR_GREATER
		Assert.Equal(expectedLocation, resTyped.Location);
#endif
	}
	
#if NET6_0
	/// <summary>
	///		Extracts value from the specified property of <see cref="IResult"/> instance
	/// </summary>
	/// <param name="res"> The <see cref="IResult"/> instance </param>
	/// <param name="propName"> The name of the property ("Value" by default) </param>
	/// <remarks>
	///		Gotta use reflection as Microsoft.AspNetCore.Http.Result.ObjectResult type is internal.
	///		Also, we can't use 'dynamic' casting due to https://stackoverflow.com/q/15016561/968003
	/// </remarks>
	private static object? GetPropValue(this IResult res, string propName = "Value")
		=> res.GetType().GetProperty(propName)?.GetValue(res);
	
	/// <summary>
	///		Extracts <see cref="ProblemDetails"/> from <see cref="IResult"/>
	/// </summary>
	private static ProblemDetails? GetProblemDetails(this IResult res) => res.GetPropValue() as ProblemDetails;
#endif
}
#endif

