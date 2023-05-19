using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='PayloadTooLarge' to <see cref="PayloadTooLargeResult"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class PayloadTooLarge : ControllerBase
{
	private readonly DomainPayloadTooLargeService _service = new ();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetPayloadTooLargeWithNoMessage()=> _service.GetPayloadTooLargeWithNoMessage().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetPayloadTooLargeWithMessage()	=> _service.GetPayloadTooLargeWithMessage().ToActionResult();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetPayloadTooLargeWithNoMessageWhenExpectedNumber()	=> _service.GetPayloadTooLargeWithNoMessageWhenExpectedNumber().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetPayloadTooLargeWithMessageWhenExpectedNumber()	=> _service.GetPayloadTooLargeWithMessageWhenExpectedNumber().ToActionResult();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetPayloadTooLargeWithNoMessageWhenExpectedNumberTuple()	=> _service.GetPayloadTooLargeWithNoMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
	public IActionResult GetPayloadTooLargeWithMessageWhenExpectedNumberTuple()	=> _service.GetPayloadTooLargeWithMessageWhenExpectedNumberTuple().ToActionResult();
}
