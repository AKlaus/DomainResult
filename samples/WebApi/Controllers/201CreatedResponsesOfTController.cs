using System.Threading.Tasks;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CreatedResponsesOfTController : ControllerBase
{
	private readonly DomainSuccessService _service = new();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<int> Get201CreatedFromTuple()
	{
		return _service.GetSuccessWithNumericValueTuple().ToCustomActionResultOfT(val => CreatedAtAction(nameof(GetById), new { id = val }, val));
	}

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public Task<ActionResult<int>> Get201CreatedFromTupleTask()
	{
		return _service.GetSuccessWithNumericValueTupleTask().ToCustomActionResultOfT(val => CreatedAtAction(nameof(GetById), new { id = val }, val));
	}

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<int> Get201Created()
	{
		return _service.GetSuccessWithNumericValue().ToCustomActionResultOfT(val => CreatedAtAction(nameof(GetById), new { id = val }, val));
	}

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public Task<ActionResult<int>> Get201CreatedTask()
	{
		return _service.GetSuccessWithNumericValueTask().ToCustomActionResultOfT(val => CreatedAtAction(nameof(GetById), new { id = val }, val));
	}

	[HttpGet("{id}")]
	public IActionResult GetById([FromRoute] int id)
	{
		return Ok(new { id });
	}
}
