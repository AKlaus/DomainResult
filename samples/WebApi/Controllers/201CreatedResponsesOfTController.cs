using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CreatedResponsesOfTController : ControllerBase
{
	private readonly DomainSuccessService _service = new();

	[HttpPost("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<int> Post201CreatedFromTuple()
		=> _service.GetSuccessWithNumericValueTuple()
		           .ToCustomActionResultOfT(val => CreatedAtAction(nameof(GetById), new { id = val }, val));

	[HttpPost("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public Task<ActionResult<int>> Post201CreatedFromTupleTask()
		=> _service.GetSuccessWithNumericValueTupleTask()
		           .ToCustomActionResultOfT(val => CreatedAtAction(nameof(GetById), new { id = val }, val));

	[HttpPost("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<int> Post201Created()
		=> _service.GetSuccessWithNumericValue()
		           .ToCustomActionResultOfT(val => CreatedAtAction(nameof(GetById), new { id = val }, val));

	[HttpPost("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public Task<ActionResult<int>> Post201CreatedTask()
		=> _service.GetSuccessWithNumericValueTask()
		           .ToCustomActionResultOfT(val => CreatedAtAction(nameof(GetById), new { id = val }, val));

	[HttpGet("{id}")]
	public IActionResult GetById([FromRoute] int id) 
		=> Ok(new { id });
}
