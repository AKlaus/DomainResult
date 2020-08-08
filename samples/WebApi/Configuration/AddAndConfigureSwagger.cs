using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace AK.DomainResult.Examples.WebApi.Configuration
{
	internal static partial class ServiceCollectionExtensions
	{
		public static void AddAndConfigureSwagger(this IServiceCollection services)
		{
			services.AddSwaggerGen(options =>
				{
					options.SwaggerDoc("v1", new OpenApiInfo { Title = "DomainResult Sample API", Version = "v1" });
				});
		}

		public static void ConfigureSwagger(this IApplicationBuilder app)
		{
			app.UseSwagger();
			app.UseSwaggerUI(c =>
				{
					c.SwaggerEndpoint("/swagger/v1/swagger.json", "DomainResult API V1");
					c.OAuthAppName("DomainResult Sample API");
				});
		}
	}
}
