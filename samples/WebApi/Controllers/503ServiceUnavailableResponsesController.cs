using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='CriticalDependencyError' to HTTP 503 Service Unavailable
/// </summary>
[ApiController]
[Route("[controller]")]
public class ServiceUnavailableResponsesController : ControllerBase
{
	private readonly DomainCriticalUnavailableService _service = new ();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public IActionResult GetCriticalDependencyErrorWithNoMessage()	=> _service.GetCriticalDependencyErrorWithNoMessage().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public IActionResult GetCriticalDependencyErrorWithMessage()	=> _service.GetCriticalDependencyErrorWithMessage().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public IActionResult GetCriticalDependencyErrorWithNoMessageWhenExpectedNumber()=> _service.GetCriticalDependencyErrorWithNoMessageWhenExpectedNumber().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public IActionResult GetCriticalDependencyErrorWithMessageWhenExpectedNumber()	=> _service.GetCriticalDependencyErrorWithMessageWhenExpectedNumber().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public IActionResult GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTuple()	=> _service.GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public IActionResult GetCriticalDependencyErrorWithMessageWhenExpectedNumberTuple()		=> _service.GetCriticalDependencyErrorWithMessageWhenExpectedNumberTuple().ToActionResult();
}
