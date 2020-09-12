using System.Linq;
using System.Threading.Tasks;

using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers
{
	/// <summary>
	///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Error' to a custom <see cref="BadRequestResult"/>
	/// </summary>
	[ApiController]
	[Route("[controller]")]
	public class BadRequestCustomErrorResponsesController : ControllerBase
	{
		private readonly DomainErrorService _service = new DomainErrorService();

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
		public IActionResult GetErrorWithCustomStatusAndMessage()
									 => _service.GetErrorWithNoMessage()
												.ToActionResult((problemDetails, state) =>
												{
													if (state.Errors?.Any() == true)
														return;
													problemDetails.Status = 422;
													problemDetails.Title = "D'oh!";
													problemDetails.Detail = "I wish devs put more efforts into it...";
												});
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public Task<IActionResult> GetErrorWithCustomTitleAndOriginalMessage()
									 => _service.GetErrorWithMessageTask()
												.ToActionResult((problemDetails, state) =>
												{
													problemDetails.Title = "D'oh!";
												});

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
		public IActionResult GetErrorWithCustomStatusAndMessageWhenExpectedNumber()
									 => _service.GetErrorWithNoMessageWhenExpectedNumber()
												.ToActionResult((problemDetails, state) =>
												{
													if (state.Errors?.Any() == true)
														return;
													problemDetails.Status = 422;
													problemDetails.Title = "D'oh!";
													problemDetails.Detail = "I wish devs put more efforts into it...";
												});
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public Task<IActionResult> GetErrorWithCustomTitleAndOriginalMessageeWhenExpectedNumber()
									 => _service.GetErrorWithMessageWhenExpectedNumberTask()
												.ToActionResult((problemDetails, state) =>
												{
													problemDetails.Title = "D'oh!";
												});

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
		public IActionResult GetErrorWithCustomStatusAndMessageWhenExpectedNumberAsTuple()
									 => _service.GetErrorWithNoMessageWhenExpectedNumberTuple()
												.ToActionResult((problemDetails, state) =>
												{
													if (state.Errors?.Any() == true)
														return;
													problemDetails.Status = 422;
													problemDetails.Title = "D'oh!";
													problemDetails.Detail = "I wish devs put more efforts into it...";
												});
		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public Task<IActionResult> GetErrorWithCustomTitleAndOriginalMessageeWhenExpectedNumberAsTuple()
									 => _service.GetErrorWithMessageWhenExpectedNumberTupleTask()
												.ToActionResult((problemDetails, state) =>
												{
													problemDetails.Title = "D'oh!";
												});

		[HttpGet("[action]")]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public Task<ActionResult<int>> GetErrorOfTWithCustomTitleAndOriginalMessageeWhenExpectedNumberAsTuple()
									 => _service.GetErrorWithMessageWhenExpectedNumberTupleTask()
												.ToActionResultOfT((problemDetails, state) =>
												{
													problemDetails.Title = "D'oh!";
												});
	}
}
