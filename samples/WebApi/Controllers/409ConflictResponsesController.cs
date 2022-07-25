using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Conflict' to <see cref="ConflictResult"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class ConflictResponsesController : ControllerBase
{
	private readonly DomainConflictService _service = new ();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public IActionResult GetConflictWithNoMessage()=> _service.GetConflictWithNoMessage().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public IActionResult GetConflictWithMessage()	=> _service.GetConflictWithMessage().ToActionResult();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public IActionResult GetConflictWithNoMessageWhenExpectedNumber()	=> _service.GetConflictWithNoMessageWhenExpectedNumber().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public IActionResult GetConflictWithMessageWhenExpectedNumber()	=> _service.GetConflictWithMessageWhenExpectedNumber().ToActionResult();

	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public IActionResult GetConflictWithNoMessageWhenExpectedNumberTuple()	=> _service.GetConflictWithNoMessageWhenExpectedNumberTuple().ToActionResult();
	[HttpPut("[action]")]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public IActionResult GetConflictWithMessageWhenExpectedNumberTuple()	=> _service.GetConflictWithMessageWhenExpectedNumberTuple().ToActionResult();
}
