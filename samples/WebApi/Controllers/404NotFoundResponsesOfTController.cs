using DomainResults.Common;
using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

/// <summary>
///		Converts <see cref="IDomainResult"/>, <see cref="IDomainResult{T}"/> and `(T, <see cref="IDomainResult"/>)` responses with <see cref="IDomainResult.Status"/>='NotFound' to <see cref="ActionResult{TValue}"/>
/// </summary>
[ApiController]
[Route("[controller]")]
public class NotFoundResponsesOfTController : ControllerBase
{
	private readonly DomainNotFoundService _service = new ();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<int> GetNotFoundWithNoMessageWhenExpectedNumber()=> _service.GetNotFoundWithNoMessageWhenExpectedNumber().ToActionResultOfT();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<int> GetNotFoundWithMessageWhenExpectedNumber()	 => _service.GetNotFoundWithMessageWhenExpectedNumber().ToActionResultOfT();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<int> GetNotFoundWithMessagesWhenExpectedNumber() => _service.GetNotFoundWithMessagesWhenExpectedNumber().ToActionResultOfT();

	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<int> GetNotFoundWithNoMessageWhenExpectedNumberTuple()	=> _service.GetNotFoundWithNoMessageWhenExpectedNumberTuple().ToActionResultOfT();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<int> GetNotFoundWithMessageWhenExpectedNumberTuple()	=> _service.GetNotFoundWithMessageWhenExpectedNumberTuple().ToActionResultOfT();
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<int> GetNotFoundWithMessagesWhenExpectedNumberTuple()	=> _service.GetNotFoundWithMessagesWhenExpectedNumberTuple().ToActionResultOfT();
}
