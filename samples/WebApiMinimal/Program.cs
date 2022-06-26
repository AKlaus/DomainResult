using Microsoft.AspNetCore.Builder;

using DomainResults.Examples.WebApiMinimal.Configuration;
using DomainResults.Examples.WebApiMinimal.Routes;

var builder = WebApplication.CreateBuilder(args);
	builder.Services.AddAndConfigureSwagger();

var app = builder.Build();
	app.UseDeveloperExceptionPage();
	app.ConfigureSwagger();

// All routes to return HTTP 200 status
app.MapSuccessResponses();
// All routes to return HTTP 201 status
app.MapSuccessCreatedResponses();
// All routes to return HTTP 204 status
app.MapSuccessNoContentResponses();
// All routes to return HTTP 400 status
app.MapBadRequestResponses();
// All routes to return HTTP 400 status with custom errors
app.MapBadRequestCustomErrorResponses();
// All routes to return HTTP 403 status
app.MapForbiddenResponses();

app.Run();