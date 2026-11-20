using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DomainResults.Tests.Mvc;

/// <summary>
///		Helper methods to run an <see cref="IActionResult"/> or an <see cref="IResult"/> and read the response
/// </summary>
public static class ResultExecutionExtension
{
	/// <summary>
	///		Runs the <see cref="IActionResult"/> as MVC does, and returns the HTTP code and the JSON body of the response
	/// </summary>
	/// <param name="actionResult"> The <see cref="IActionResult"/> to run </param>
	/// <param name="configureServices"> Optional services to register after <c>AddControllers()</c> </param>
	public static async Task<(int statusCode, JsonElement body)> ExecuteToJsonAsync(this IActionResult actionResult, Action<IServiceCollection>? configureServices = null)
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
	/// <param name="result"> The <see cref="IResult"/> to run </param>
	public static async Task<(int statusCode, JsonElement body)> ExecuteToJsonAsync(this IResult result)
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
}
