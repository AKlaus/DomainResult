using System.Threading.Tasks;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class SuccessResponsesController : ControllerBase
{
	private readonly DomainSuccessService _service = new();

	[HttpGet("[action]")]
	public IActionResult Get200OkWithNumber()
	{
		return _service.GetSuccessWithNumericValue().ToActionResult();
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