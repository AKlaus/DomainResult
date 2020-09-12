using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers
{
	/// <summary>
	///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Error' to <see cref="BadRequestResult"/>
	/// </summary>
	[ApiController]
	[Route("[controller]")]
	public class BadRequestResponsesController : ControllerBase
	{
		private readonly DomainErrorService _service = new DomainErrorService();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithNoMessage()=> _service.GetErrorWithNoMessage().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithMessage()	=> _service.GetErrorWithMessage().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithMessages()	=> _service.GetErrorWithMessages().ToActionResult();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithNoMessageWhenExpectedNumber()	=> _service.GetErrorWithNoMessageWhenExpectedNumber().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithMessageWhenExpectedNumber()	=> _service.GetErrorWithMessageWhenExpectedNumber().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithMessagesWhenExpectedNumber()	=> _service.GetErrorWithMessagesWhenExpectedNumber().ToActionResult();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithNoMessageWhenExpectedNumberTuple()	=> _service.GetErrorWithNoMessageWhenExpectedNumberTuple().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithMessageWhenExpectedNumberTuple()	=> _service.GetErrorWithMessageWhenExpectedNumberTuple().ToActionResult();
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult GetErrorWithMessagesWhenExpectedNumberTuple()	=> _service.GetErrorWithMessagesWhenExpectedNumberTuple().ToActionResult();
	}
}
