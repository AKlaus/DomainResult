using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Unauthorized' to <see cref="ForbidResult"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class ForbiddenResponsesController : ControllerBase
{
	private readonly DomainUnauthorizedService _service = new ();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public IActionResult GetUnauthorizedWithNoMessage()=> _service.GetUnauthorizedWithNoMessage().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public IActionResult GetUnauthorizedWithMessage()	=> _service.GetUnauthorizedWithMessage().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public IActionResult GetUnauthorizedWithNoMessageWhenExpectedNumber()	=> _service.GetUnauthorizedWithNoMessageWhenExpectedNumber().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public IActionResult GetUnauthorizedWithMessageWhenExpectedNumber()	=> _service.GetUnauthorizedWithMessageWhenExpectedNumber().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public IActionResult GetUnauthorizedWithNoMessageWhenExpectedNumberTuple()	=> _service.GetUnauthorizedWithNoMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public IActionResult GetUnauthorizedWithMessageWhenExpectedNumberTuple()	=> _service.GetUnauthorizedWithMessageWhenExpectedNumberTuple().ToActionResult();
}
