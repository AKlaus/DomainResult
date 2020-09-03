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
			return _service.GetSuccessWithNumericValueTuple().ToCustomActionResult(val => CreatedAtRoute(nameof(GetById), new { id = val }, val));
		}

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public Task<ActionResult> Get201CreatedFromTupleTask()
		{
			return _service.GetSuccessWithNumericValueTupleTask().ToCustomActionResult(val => CreatedAtRoute(nameof(GetById), new { id = val }, val));
		}

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public IActionResult Get201Created()
		{
			return _service.GetSuccessWithNumericValue().ToCustomActionResult(val => CreatedAtRoute(nameof(GetById), new { id = val }, val));
		}

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public Task<ActionResult> Get201CreatedTask()
		{
			return _service.GetSuccessWithNumericValueTask().ToCustomActionResult(val => CreatedAtRoute(nameof(GetById), new { id = val }, val));
		}

		[HttpGet("{id}", Name = nameof(GetById))]
		public IActionResult GetById([FromRoute] int id)
		{
			return Ok(new { id });
		}
	}
}
