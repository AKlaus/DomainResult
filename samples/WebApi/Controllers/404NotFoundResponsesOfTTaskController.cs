using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="Task"/> of <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='NotFound' to <see cref="ActionResult{TValue}"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class NotFoundResponsesOfTTaskController : ControllerBase
{
	private readonly DomainNotFoundService _service = new DomainNotFoundService();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<ActionResult<int>> GetNotFoundWithNoMessageWhenExpectedNumberTask()	=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTask().ToActionResultOfT();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<ActionResult<int>> GetNotFoundWithMessageWhenExpectedNumberTask()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTask().ToActionResultOfT();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<ActionResult<int>> GetNotFoundWithMessagesWhenExpectedNumberTask()	=> _service.GetNotFoundWithMessagesWhenExpectedNumberTask().ToActionResultOfT();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<ActionResult<int>> GetNotFoundWithNoMessageWhenExpectedNumberTupleTask()=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTupleTask().ToActionResultOfT();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<ActionResult<int>> GetNotFoundWithMessageWhenExpectedNumberTupleTask()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTupleTask().ToActionResultOfT();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<ActionResult<int>> GetNotFoundWithMessagesWhenExpectedNumberTupleTask() => _service.GetNotFoundWithMessagesWhenExpectedNumberTupleTask().ToActionResultOfT();
}