using System.Threading.Tasks;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
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
	{
#pragma warning disable CS8603 // Possible null reference return.
		return _service.GetSuccess().ToActionResult() as NoContentResult;
#pragma warning restore CS8603 // Possible null reference return.
	}
	
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public Task<IActionResult> Get204NoContentTask()
	{
		return _service.GetSuccessTask().ToActionResult();
	}
}
