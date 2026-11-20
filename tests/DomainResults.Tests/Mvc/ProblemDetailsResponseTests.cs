using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DomainResults.Tests.Mvc;

/// <summary>
///		Tests for the <see cref="ProblemDetails"/> body that the <see cref="IActionResult"/> and the <see cref="IResult"/> conversions write
/// </summary>
public class ProblemDetailsResponseTests
{
	[Theory]
	[MemberData(nameof(FailedStatusCases))]
	public async Task ToActionResult_FailedStatus_WritesBodyOfToResult(IDomainResult domainResult, int expectedCode, string expectedTitle)
	{
		// GIVEN a domain result with a failed status and an error message

		// WHEN the IActionResult and the IResult conversions run
		var (actionStatusCode, actionBody) = await ExecuteAsync(domainResult.ToActionResult());
		var (resultStatusCode, resultBody) = await ExecuteAsync(domainResult.ToResult());

		// THEN the IActionResult conversion writes the HTTP code, title and detail of the convention
		Assert.Equal(expectedCode, actionStatusCode);
		Assert.Equal(expectedCode, actionBody.GetProperty("status").GetInt32());
		Assert.Equal(expectedTitle, actionBody.GetProperty("title").GetString());
		Assert.Equal("1", actionBody.GetProperty("detail").GetString());
		// and a 'traceId', as 'ControllerBase.Problem()' does
		Assert.False(string.IsNullOrEmpty(actionBody.GetProperty("traceId").GetString()));
		// and the same HTTP code and 'type' as the IResult conversion
		Assert.Equal(resultStatusCode, actionStatusCode);
		Assert.Equal(GetOptionalString(resultBody, "type"), GetOptionalString(actionBody, "type"));
	}
	public static readonly TheoryData<IDomainResult, int, string> FailedStatusCases = new()
		{
			{ DomainResult.Failed("1"),					HttpCodeConvention.FailedHttpCode,					HttpCodeConvention.FailedProblemDetailsTitle },
			{ DomainResult.NotFound("1"),				HttpCodeConvention.NotFoundHttpCode,				HttpCodeConvention.NotFoundProblemDetailsTitle },
			{ DomainResult.Unauthorized("1"),			HttpCodeConvention.UnauthorizedHttpCode,			HttpCodeConvention.UnauthorizedProblemDetailsTitle },
			{ DomainResult.Conflict("1"),				HttpCodeConvention.ConflictHttpCode,				HttpCodeConvention.ConflictProblemDetailsTitle },
			{ DomainResult.ContentTooLarge("1"),		HttpCodeConvention.ContentTooLargeHttpCode,			HttpCodeConvention.ContentTooLargeProblemDetailsTitle },
			{ DomainResult.CriticalDependencyError("1"),HttpCodeConvention.CriticalDependencyErrorHttpCode, HttpCodeConvention.CriticalDependencyErrorProblemDetailsTitle },
		};

	[Fact]
	public async Task ToActionResult_NotFound_WritesRfcType()
	{
		// GIVEN a domain result with the 'NotFound' status
		var domainResult = DomainResult.NotFound("1");

		// WHEN the IActionResult and the IResult conversions run
		var (_, actionBody) = await ExecuteAsync(domainResult.ToActionResult());
		var (_, resultBody) = await ExecuteAsync(domainResult.ToResult());

		// THEN both conversions write the RFC link of the 404 code as 'type'
		Assert.Equal(ExpectedNotFoundType, actionBody.GetProperty("type").GetString());
		Assert.Equal(ExpectedNotFoundType, resultBody.GetProperty("type").GetString());
	}

	[Theory]
	[MemberData(nameof(ResultShapeCases))]
	[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "The shape names the test case")]
	public async Task ToActionResult_ResultShape_WritesTypeAndTraceId(string shape, Func<IActionResult> convert)
	{
		// GIVEN a domain result of the given shape with the 'NotFound' status

		// WHEN the IActionResult conversion of the shape runs
		var (statusCode, body) = await ExecuteAsync(convert());

		// THEN the body has the convention values, the 'type' and a 'traceId'
		Assert.Equal(HttpCodeConvention.NotFoundHttpCode, statusCode);
		Assert.Equal(HttpCodeConvention.NotFoundProblemDetailsTitle, body.GetProperty("title").GetString());
		Assert.Equal("1", body.GetProperty("detail").GetString());
		Assert.Equal(ExpectedNotFoundType, body.GetProperty("type").GetString());
		Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
	}
	public static readonly TheoryData<string, Func<IActionResult>> ResultShapeCases = new()
		{
			{ "IDomainResult<T>.ToActionResult()",		() => DomainResult.NotFound<int>("1").ToActionResult() },
			{ "(T, IDomainResult).ToActionResult()",	() => (10, DomainResult.NotFound("1")).ToActionResult() },
			{ "IDomainResult<T>.ToActionResultOfT()",	() => ToActionResult(DomainResult.NotFound<int>("1").ToActionResultOfT()) },
			{ "(T, IDomainResult).ToActionResultOfT()",	() => ToActionResult((10, DomainResult.NotFound("1")).ToActionResultOfT()) },
			{ "IDomainResult<T>.ToCustomActionResult()",() => DomainResult.NotFound<int>("1").ToCustomActionResult(v => new CreatedResult("/", v)) },
		};

	[Fact]
	public async Task ToActionResult_ProblemDetailsCustomisation_AppliesCustomisation()
	{
		// GIVEN 'AddProblemDetails()' with a customisation that adds an extension
		var domainResult = DomainResult.NotFound("1");

		// WHEN the IActionResult conversion runs
		var (_, body) = await ExecuteAsync(domainResult.ToActionResult(),
		                                   services => services.AddProblemDetails(o => o.CustomizeProblemDetails = c => c.ProblemDetails.Extensions["customised"] = true));

		// THEN the body has the extension of the customisation
		Assert.True(body.GetProperty("customised").GetBoolean());
	}

	[Fact]
	public async Task ToActionResult_ErrorActionSetsFields_KeepsErrorActionValues()
	{
		// GIVEN an 'errorAction' that sets the status, 'type' and an extension
		var domainResult = DomainResult.Failed("1");

		// WHEN the IActionResult conversion runs
		var (statusCode, body) = await ExecuteAsync(domainResult.ToActionResult((problemDetails, _) =>
			{
				problemDetails.Status = 422;
				problemDetails.Type = "https://example.com/errors/validation";
				problemDetails.Extensions["code"] = "E1";
			}));

		// THEN the body keeps the values of the 'errorAction'
		Assert.Equal(422, statusCode);
		Assert.Equal(422, body.GetProperty("status").GetInt32());
		Assert.Equal("https://example.com/errors/validation", body.GetProperty("type").GetString());
		Assert.Equal("E1", body.GetProperty("code").GetString());
		// and the 'traceId' of the factory
		Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
	}

	#region Auxiliary methods [PRIVATE] -----------------------------------

#if NET8_0_OR_GREATER
	private const string ExpectedNotFoundType = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
#else
	private const string ExpectedNotFoundType = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
#endif

	private static IActionResult ToActionResult<T>(ActionResult<T> actionResult) => ((IConvertToActionResult)actionResult).Convert();

	private static string? GetOptionalString(JsonElement body, string propertyName)
		=> body.TryGetProperty(propertyName, out var property) ? property.GetString() : null;

	/// <summary>
	///		Runs the <see cref="IActionResult"/> as MVC does, and returns the HTTP code and the JSON body of the response
	/// </summary>
	private static async Task<(int statusCode, JsonElement body)> ExecuteAsync(IActionResult actionResult, Action<IServiceCollection>? configureServices = null)
	{
		var httpContext = CreateHttpContext(services =>
			{
				services.AddControllers();
				configureServices?.Invoke(services);
			});
		await actionResult.ExecuteResultAsync(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));
		return ReadResponse(httpContext);
	}

	/// <summary>
	///		Runs the <see cref="IResult"/> as the minimal API does, and returns the HTTP code and the JSON body of the response
	/// </summary>
	private static async Task<(int statusCode, JsonElement body)> ExecuteAsync(IResult result)
	{
		var httpContext = CreateHttpContext(_ => { });
		await result.ExecuteAsync(httpContext);
		return ReadResponse(httpContext);
	}

	private static HttpContext CreateHttpContext(Action<IServiceCollection> configureServices)
	{
		var services = new ServiceCollection().AddLogging();
		configureServices(services);
		return new DefaultHttpContext
			{
				RequestServices = services.BuildServiceProvider(),
				Response = { Body = new MemoryStream() }
			};
	}

	private static (int statusCode, JsonElement body) ReadResponse(HttpContext httpContext)
	{
		httpContext.Response.Body.Position = 0;
		using var document = JsonDocument.Parse(httpContext.Response.Body);
		return (httpContext.Response.StatusCode, document.RootElement.Clone());
	}

	#endregion // Auxiliary methods [PRIVATE] -----------------------------
}
