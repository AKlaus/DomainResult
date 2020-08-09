using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using AK.DomainResults.Examples.Domain;
using AK.DomainResults.Mvc;

namespace AK.DomainResults.Examples.WebApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class NotFoundResponsesController : ControllerBase
	{
		private readonly DomainNotFoundService _service = new DomainNotFoundService();

		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithNoMessage()	=> _service.GetNotFoundWithNoMessage().ToActionResult();
		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithMessage()	=> _service.GetNotFoundWithMessage().ToActionResult();
		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithMessages()	=> _service.GetNotFoundWithMessages().ToActionResult();

		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithNoMessageTask() => _service.GetNotFoundWithNoMessageTask().ToActionResult();
		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithMessageTask()	  => _service.GetNotFoundWithMessageTask().ToActionResult();
		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithMessagesTask()  => _service.GetNotFoundWithMessagesTask().ToActionResult();

		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithNoMessageWhenExpectedNumber()=> _service.GetNotFoundWithNoMessageWhenExpectedNumber().ToActionResult();
		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithMessageWhenExpectedNumber()	 => _service.GetNotFoundWithMessageWhenExpectedNumber().ToActionResult();
		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithMessagesWhenExpectedNumber() => _service.GetNotFoundWithMessagesWhenExpectedNumber().ToActionResult();

		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithNoMessageWhenExpectedNumberTask()	=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTask().ToActionResult();
		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithMessageWhenExpectedNumberTask()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTask().ToActionResult();
		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithMessagesWhenExpectedNumberTask()	=> _service.GetNotFoundWithMessagesWhenExpectedNumberTask().ToActionResult();

		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithNoMessageWhenExpectedNumberTuple()	=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTuple().ToActionResult();
		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithMessageWhenExpectedNumberTuple()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTuple().ToActionResult();
		[HttpGet("[action]")]
		public IActionResult GetNotFoundWithMessagesWhenExpectedNumberTuple()	=> _service.GetNotFoundWithMessagesWhenExpectedNumberTuple().ToActionResult();

		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithNoMessageWhenExpectedNumberTupleTask()=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTupleTask().ToActionResult();
		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithMessageWhenExpectedNumberTupleTask()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTupleTask().ToActionResult();
		[HttpGet("[action]")]
		public Task<IActionResult> GetNotFoundWithMessagesWhenExpectedNumberTupleTask() => _service.GetNotFoundWithMessagesWhenExpectedNumberTupleTask().ToActionResult();
	}
}
