using System.Linq;
using System.Threading.Tasks;

using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='Failed' to a custom <see cref="BadRequestResult"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class BadRequestCustomErrorResponsesController : ControllerBase
{
	private readonly DomainFailedService _service = new ();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public IActionResult GetErrorWithCustomStatusAndMessage()
								 => _service.GetFailedWithNoMessage()
											.ToActionResult((problemDetails, state) =>
											{
												if (state.Errors.Any())
													return;
												problemDetails.Status = 422;
												problemDetails.Title = "D'oh!";
												problemDetails.Detail = "I wish devs put more efforts into it...";
											});
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public Task<IActionResult> GetErrorWithCustomTitleAndOriginalMessage()
								 => _service.GetFailedWithMessageTask()
											.ToActionResult((problemDetails, _) =>
											{
												problemDetails.Title = "D'oh!";
											});

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public IActionResult GetErrorWithCustomStatusAndMessageWhenExpectedNumber()
								 => _service.GetFailedWithNoMessageWhenExpectedNumber()
											.ToActionResult((problemDetails, state) =>
											{
												if (state.Errors.Any())
													return;
												problemDetails.Status = 422;
												problemDetails.Title = "D'oh!";
												problemDetails.Detail = "I wish devs put more efforts into it...";
											});
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public Task<IActionResult> GetErrorWithCustomTitleAndOriginalMessageWhenExpectedNumber()
								 => _service.GetFailedWithMessageWhenExpectedNumberTask()
											.ToActionResult((problemDetails, _) =>
											{
												problemDetails.Title = "D'oh!";
											});

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public IActionResult GetErrorWithCustomStatusAndMessageWhenExpectedNumberAsTuple()
								 => _service.GetFailedWithNoMessageWhenExpectedNumberTuple()
											.ToActionResult((problemDetails, state) =>
											{
												if (state.Errors.Any())
													return;
												problemDetails.Status = 422;
												problemDetails.Title = "D'oh!";
												problemDetails.Detail = "I wish devs put more efforts into it...";
											});
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public Task<IActionResult> GetErrorWithCustomTitleAndOriginalMessageWhenExpectedNumberAsTuple()
								 => _service.GetFailedWithMessageWhenExpectedNumberTupleTask()
											.ToActionResult((problemDetails, _) =>
											{
												problemDetails.Title = "D'oh!";
											});

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public Task<ActionResult<int>> GetErrorOfTWithCustomTitleAndOriginalMessageWhenExpectedNumberAsTuple()
								 => _service.GetFailedWithMessageWhenExpectedNumberTupleTask()
											.ToActionResultOfT((problemDetails, _) =>
											{
												problemDetails.Title = "D'oh!";
											});
}
