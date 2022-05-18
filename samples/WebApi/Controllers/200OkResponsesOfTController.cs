using System.Threading.Tasks;

using DomainResults.Examples.Domain;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class SuccessResponsesOfTController : ControllerBase
{
	private readonly DomainSuccessService _service = new();
	[HttpGet("[action]")]
	public ActionResult<int> Get200OkWithExplicitNumber()
	{
		return _service.GetSuccessWithNumericValue().ToActionResultOfT();
	}

	[HttpGet("[action]")]
	public Task<ActionResult<int>> Get200OkWithExplicitNumberTask()
	{
		return _service.GetSuccessWithNumericValueTask().ToActionResultOfT();
	}

	[HttpGet("[action]")]
	public ActionResult<int> Get200OkTupleWithExplicitNumber()
	{
		var (res, error) = _service.GetSuccessWithNumericValueTuple();
		return (res, error).ToActionResultOfT();
	}

	[HttpGet("[action]")]
	public Task<ActionResult<int>> Get200OkTupleWithExplicitNumberTask()
	{
		return _service.GetSuccessWithNumericValueTupleTask().ToActionResultOfT();
	}
}
