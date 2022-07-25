using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Failed' to <see cref="BadRequestResult"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class BadRequestResponsesController : ControllerBase
{
	private readonly DomainFailedService _service = new ();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithNoMessage()=> _service.GetFailedWithNoMessage().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithMessage()	=> _service.GetFailedWithMessage().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithMessages()	=> _service.GetFailedWithMessages().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithNoMessageWhenExpectedNumber()	=> _service.GetFailedWithNoMessageWhenExpectedNumber().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithMessageWhenExpectedNumber()	=> _service.GetFailedWithMessageWhenExpectedNumber().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithMessagesWhenExpectedNumber()	=> _service.GetFailedWithMessagesWhenExpectedNumber().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithNoMessageWhenExpectedNumberTuple()	=> _service.GetFailedWithNoMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithMessageWhenExpectedNumberTuple()	=> _service.GetFailedWithMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult GetErrorWithMessagesWhenExpectedNumberTuple()	=> _service.GetFailedWithMessagesWhenExpectedNumberTuple().ToActionResult();
}
