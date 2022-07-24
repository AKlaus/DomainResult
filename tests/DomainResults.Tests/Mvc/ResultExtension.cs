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
	///		Extracts <see cref="ProblemDetails"/> from <see cref="IResult"/>
	/// </summary>
	public static ProblemDetails? GetProblemDetails(this IResult res) => res.GetPropValue() as ProblemDetails;
	
	/// <summary>
	///		Extracts value from the specified property of <see cref="IResult"/> instance
	/// </summary>
	/// <param name="res"> The <see cref="IResult"/> instance </param>
	/// <param name="propName"> The name of the property ("Value" by default) </param>
	/// <remarks>
	///		Gotta use reflection as Microsoft.AspNetCore.Http.Result.ObjectResult type is internal.
	///		Also, we can't use 'dynamic' casting due to https://stackoverflow.com/q/15016561/968003
	/// </remarks>
	public static object? GetPropValue(this IResult res, string propName = "Value")
		=> res.GetType().GetProperty(propName)?.GetValue(res);
	
	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'ObjectResult' class
	/// </summary>
	public static void AssertObjectResultType(this IResult res)
		=> Assert.Equal("Microsoft.AspNetCore.Http.Result.ObjectResult", res.GetType().FullName);

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'OkObjectResult' class
	/// </summary>
	public static void AssertOkObjectResultType(this IResult res)
		=> Assert.Equal("Microsoft.AspNetCore.Http.Result.OkObjectResult", res.GetType().FullName);

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'NoContentResult' class
	/// </summary>
	public static void AssertNoContentResultType(this IResult res)
		=> Assert.Equal("Microsoft.AspNetCore.Http.Result.NoContentResult", res.GetType().FullName);

	/// <summary>
	///		Assert that the <see cref="IResult"/> instance represent an internal 'CreatedResult' class
	/// </summary>
	public static void AssertCreatedResultType(this IResult res)
		=> Assert.Equal("Microsoft.AspNetCore.Http.Result.CreatedResult", res.GetType().FullName);

	// NOTE that we don't check Microsoft.AspNetCore.Http.Result.ObjectResult.StatusCode property as it always remains NULL. 
	// TODO: Double check if ObjectResult.StatusCode remains NULL in .NET 7 as well.
}
#endif

