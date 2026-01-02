using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DomainResults.Examples.WebApiMinimal.Configuration;

internal static class ServiceCollectionExtensions
{
	public static void AddAndConfigureSwagger(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();
		services.AddOpenApiDocument(s =>
			{
				s.Title = "DomainResult Sample Minimal API";
				s.Version = "v1";
			});
	}
	
	public static void ConfigureSwagger(this IApplicationBuilder app)
	{
		app.UseOpenApi();
		app.UseSwaggerUi();
	}
}
