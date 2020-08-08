using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using AK.DomainResults.Examples.Domain;
using AK.DomainResults.Mvc;

namespace AK.DomainResults.Examples.WebApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class SuccessResponsesController : ControllerBase
	{
		private readonly TupleServiceSync _service = new TupleServiceSync();

		[HttpGet("[action]")]
		public NoContentResult Get204NoContent()
		{
			return _service.GetSuccess().ToActionResult() as NoContentResult;
		}
		[HttpGet("[action]")]
		public Task<IActionResult> Get204NoContentTask()
		{
			return _service.GetSuccessTask().ToActionResultTask();
		}

		[HttpGet("[action]")]
		public IActionResult Get200OkWithNumber()
		{
			return _service.GetSuccessWithNumericValue().ToActionResult();// as OkResult;
		}
		[HttpGet("[action]")]
		public Task<IActionResult> Get200OkWithNumberTask()
		{
			return _service.GetSuccessWithNumericValueTask().ToActionResultTask();
		}

		[HttpGet]
		public IActionResult GetSuccess2()
		{
			var (res, error) = _service.GetSuccessWithNumericValueTuple();

			return (res, error).ToActionResult();
		}
	}
}
