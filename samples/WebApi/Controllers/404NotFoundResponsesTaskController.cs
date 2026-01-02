using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="Task"/> of <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='NotFound' to <see cref="NotFoundResult"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class NotFoundResponsesTaskController : ControllerBase
{
	private readonly DomainNotFoundService _service = new DomainNotFoundService();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithNoMessageTask() => _service.GetNotFoundWithNoMessageTask().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithMessageTask()	  => _service.GetNotFoundWithMessageTask().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithMessagesTask()  => _service.GetNotFoundWithMessagesTask().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithNoMessageWhenExpectedNumberTask()	=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTask().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithMessageWhenExpectedNumberTask()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTask().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithMessagesWhenExpectedNumberTask()	=> _service.GetNotFoundWithMessagesWhenExpectedNumberTask().ToActionResult();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithNoMessageWhenExpectedNumberTupleTask()=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTupleTask().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithMessageWhenExpectedNumberTupleTask()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTupleTask().ToActionResult();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetNotFoundWithMessagesWhenExpectedNumberTupleTask() => _service.GetNotFoundWithMessagesWhenExpectedNumberTupleTask().ToActionResult();
}
