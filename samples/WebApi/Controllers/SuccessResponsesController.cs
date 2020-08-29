using System.Threading.Tasks;

using AK.DomainResults.Examples.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AK.DomainResults.Examples.WebApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class SuccessResponsesController : ControllerBase
	{
		private readonly DomainSuccessService _service = new DomainSuccessService();

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

		[HttpGet("[action]")]
		public OkResult Get200OkWithNumber()
		{
#pragma warning disable CS8603 // Possible null reference return.
			return _service.GetSuccessWithNumericValue().ToActionResult() as OkResult;
#pragma warning restore CS8603 // Possible null reference return.
		}
		[HttpGet("[action]")]
		public Task<IActionResult> Get200OkWithNumberTask()
		{
			return _service.GetSuccessWithNumericValueTask().ToActionResult();
		}

		[HttpGet("[action]")]
		public IActionResult Get200OkTupleWithNumber()
		{
			var (res, error) = _service.GetSuccessWithNumericValueTuple();
			return (res, error).ToActionResult();
		}

		[HttpGet("[action]")]
		public Task<IActionResult> Get200OkTupleWithNumberTask()
		{
			return _service.GetSuccessWithNumericValueTupleTask().ToActionResult();
		}
	}
}
