using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='ContentTooLarge' to <see cref="HttpCodeConvention.ContentTooLargeHttpCode"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class ContentTooLarge : ControllerBase
{
	private readonly DomainContentTooLargeService _service = new ();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithNoMessage()=> _service.GetContentTooLargeWithNoMessage().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithMessage()	=> _service.GetContentTooLargeWithMessage().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithNoMessageWhenExpectedNumber()	=> _service.GetContentTooLargeWithNoMessageWhenExpectedNumber().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithMessageWhenExpectedNumber()	=> _service.GetContentTooLargeWithMessageWhenExpectedNumber().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithNoMessageWhenExpectedNumberTuple()	=> _service.GetContentTooLargeWithNoMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithMessageWhenExpectedNumberTuple()	=> _service.GetContentTooLargeWithMessageWhenExpectedNumberTuple().ToActionResult();
}
