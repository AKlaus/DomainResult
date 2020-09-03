using System.Threading.Tasks;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class NotFoundResponsesController : ControllerBase
	{
		private readonly DomainNotFoundService _service = new DomainNotFoundService();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithNoMessage()	=> _service.GetNotFoundWithNoMessage().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithMessage()	=> _service.GetNotFoundWithMessage().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithMessages()	=> _service.GetNotFoundWithMessages().ToActionResult();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithNoMessageTask() => _service.GetNotFoundWithNoMessageTask().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithMessageTask()	  => _service.GetNotFoundWithMessageTask().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithMessagesTask()  => _service.GetNotFoundWithMessagesTask().ToActionResult();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithNoMessageWhenExpectedNumber()=> _service.GetNotFoundWithNoMessageWhenExpectedNumber().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithMessageWhenExpectedNumber()	 => _service.GetNotFoundWithMessageWhenExpectedNumber().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithMessagesWhenExpectedNumber() => _service.GetNotFoundWithMessagesWhenExpectedNumber().ToActionResult();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithNoMessageWhenExpectedNumberTask()	=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTask().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithMessageWhenExpectedNumberTask()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTask().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithMessagesWhenExpectedNumberTask()	=> _service.GetNotFoundWithMessagesWhenExpectedNumberTask().ToActionResult();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithNoMessageWhenExpectedNumberTuple()	=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTuple().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithMessageWhenExpectedNumberTuple()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTuple().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public IActionResult GetNotFoundWithMessagesWhenExpectedNumberTuple()	=> _service.GetNotFoundWithMessagesWhenExpectedNumberTuple().ToActionResult();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithNoMessageWhenExpectedNumberTupleTask()=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTupleTask().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithMessageWhenExpectedNumberTupleTask()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTupleTask().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public Task<IActionResult> GetNotFoundWithMessagesWhenExpectedNumberTupleTask() => _service.GetNotFoundWithMessagesWhenExpectedNumberTupleTask().ToActionResult();
	}
}
