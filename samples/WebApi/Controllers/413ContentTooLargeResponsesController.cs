using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;
// BONUS ff

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='ContentTooLarge' to <see cref="ContentTooLargeResult"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class ContentTooLarge : ControllerBase
{
	private readonly DomainContentTooLargeService _service = new ();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithNoMessage()=> _service.GetContentTooLargeWithNoMessage().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithMessage()	=> _service.GetContentTooLargeWithMessage().ToActionResult();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithNoMessageWhenExpectedNumber()	=> _service.GetContentTooLargeWithNoMessageWhenExpectedNumber().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithMessageWhenExpectedNumber()	=> _service.GetContentTooLargeWithMessageWhenExpectedNumber().ToActionResult();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithNoMessageWhenExpectedNumberTuple()	=> _service.GetContentTooLargeWithNoMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetContentTooLargeWithMessageWhenExpectedNumberTuple()	=> _service.GetContentTooLargeWithMessageWhenExpectedNumberTuple().ToActionResult();
}
