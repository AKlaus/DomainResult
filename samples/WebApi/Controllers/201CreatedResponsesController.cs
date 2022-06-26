using System.Threading.Tasks;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CreatedResponsesController : ControllerBase
{
	private readonly DomainSuccessService _service = new();

	[HttpPost("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public IActionResult Post201CreatedFromTuple()
		=> _service.GetSuccessWithNumericValueTuple()
		           .ToCustomActionResult(val => CreatedAtAction(nameof(GetById), new { id = val }, val));

	[HttpPost("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public Task<IActionResult> Post201CreatedFromTupleTask()
		=> _service.GetSuccessWithNumericValueTupleTask()
		           .ToCustomActionResult(val => CreatedAtAction(nameof(GetById), new { id = val }, val));

	[HttpPost("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public IActionResult Post201Created()
		=> _service.GetSuccessWithNumericValue()
		           .ToCustomActionResult(val => CreatedAtAction(nameof(GetById), new { id = val }, val));

	[HttpPost("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public Task<IActionResult> Post201CreatedTask()
		=> _service.GetSuccessWithNumericValueTask()
		           .ToCustomActionResult(val => CreatedAtAction(nameof(GetById), new { id = val }, val));

	[HttpGet("{id}")]
	public IActionResult GetById([FromRoute] int id) => Ok(new { id });
}
