using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='NotFound' to <see cref="NotFoundResult"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class NotFoundResponsesController : ControllerBase
{
	private readonly DomainNotFoundService _service = new ();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithNoMessage()	=> _service.GetNotFoundWithNoMessage().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithMessage()	=> _service.GetNotFoundWithMessage().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithMessages()	=> _service.GetNotFoundWithMessages().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithNoMessageWhenExpectedNumber()=> _service.GetNotFoundWithNoMessageWhenExpectedNumber().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithMessageWhenExpectedNumber()	 => _service.GetNotFoundWithMessageWhenExpectedNumber().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithMessagesWhenExpectedNumber() => _service.GetNotFoundWithMessagesWhenExpectedNumber().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithNoMessageWhenExpectedNumberTuple()	=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithMessageWhenExpectedNumberTuple()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetNotFoundWithMessagesWhenExpectedNumberTuple()	=> _service.GetNotFoundWithMessagesWhenExpectedNumberTuple().ToActionResult();
}
