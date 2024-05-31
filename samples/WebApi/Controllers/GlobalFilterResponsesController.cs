using System.Threading.Tasks;

using DomainResults.Common;
using DomainResults.Examples.Domain;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainResults.Examples.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GlobalFilterResponsesController : ControllerBase
{
	private readonly DomainSuccessService _service = new();

	[HttpGet("[action]")]
	public IDomainResult<int> Get200OkWithNumber() 
		=> _service.GetSuccessWithNumericValue();

	[HttpGet("[action]")]
	public Task<IDomainResult<int>> Get200OkWithNumberTask() 
		=> _service.GetSuccessWithNumericValueTask();
	
	[HttpGet("[action]")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public Task<IDomainResult> Get204NoContentTask()
		=> _service.GetSuccessTask();
}