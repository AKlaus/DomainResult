using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class NoContentResponsesController : ControllerBase
{
	private readonly DomainSuccessService _service = new();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public NoContentResult Get204NoContent()
		=> (_service.GetSuccess().ToActionResult() as NoContentResult)!;
	
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public Task<IActionResult> Get204NoContentTask()
		=> _service.GetSuccessTask().ToActionResult();
}
