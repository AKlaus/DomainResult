using Microsoft.AspNetCore.Builder;

using DomainResults.Examples.WebApiMinimal.Configuration;
using DomainResults.Examples.WebApiMinimal.Routes;

var builder = WebApplication.CreateBuilder(args);
	builder.Services.AddAndConfigureSwagger();

var app = builder.Build();
	app.UseDeveloperExceptionPage();
	app.ConfigureSwagger();

// All routes for HTTP 200 status
app.MapSuccessResponses();
// All routes for HTTP 201 status
app.MapSuccessCreatedResponses();
// All routes for HTTP 204 status
app.MapSuccessNoContentResponses();
// All routes for HTTP 400 status
app.MapBadRequestResponses();
// All routes for HTTP 400 status with custom errors
app.MapBadRequestCustomErrorResponses();
// All routes for HTTP 403 status
app.MapForbiddenResponses();
// All routes for HTTP 404 status
app.MapNotFoundResponses();
// All routes for HTTP 409 status
app.MapConflictResponses();
// All routes for HTTP 413 status
app.MapPayloadTooLargeResponses();
// All routes for HTTP 503 status
app.MapServiceUnavailableResponses();

app.Run();