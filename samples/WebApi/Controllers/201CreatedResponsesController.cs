using System.Threading.Tasks;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class CreatedResponsesController : ControllerBase
	{
		private readonly DomainSuccessService _service = new DomainSuccessService();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public IActionResult Get201CreatedFromTuple()
		{
			return _service.GetSuccessWithNumericValueTuple().ToCustomActionResult(val => CreatedAtAction(nameof(GetById), new { id = val }, val));
		}

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public Task<IActionResult> Get201CreatedFromTupleTask()
		{
			return _service.GetSuccessWithNumericValueTupleTask().ToCustomActionResult(val => CreatedAtAction(nameof(GetById), new { id = val }, val));
		}

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public IActionResult Get201Created()
		{
			return _service.GetSuccessWithNumericValue().ToCustomActionResult(val => CreatedAtAction(nameof(GetById), new { id = val }, val));
		}

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public Task<IActionResult> Get201CreatedTask()
		{
			return _service.GetSuccessWithNumericValueTask().ToCustomActionResult(val => CreatedAtAction(nameof(GetById), new { id = val }, val));
		}

		[HttpGet("{id}")]
		public IActionResult GetById([FromRoute] int id)
		{
			return Ok(new { id });
		}
	}
}
