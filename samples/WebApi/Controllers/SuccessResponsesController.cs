﻿using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using AK.DomainResults.Examples.Domain;
using AK.DomainResults.Mvc;

namespace AK.DomainResults.Examples.WebApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class SuccessResponsesController : ControllerBase
	{
		private readonly DomainSuccessService _service = new DomainSuccessService();

		[HttpGet("[action]")]
		public NoContentResult Get204NoContent()
		{
			return _service.GetSuccess().ToActionResult() as NoContentResult;
		}
		[HttpGet("[action]")]
		public Task<IActionResult> Get204NoContentTask()
		{
			return _service.GetSuccessTask().ToActionResult();
		}

		[HttpGet("[action]")]
		public OkResult Get200OkWithNumber()
		{
			return _service.GetSuccessWithNumericValue().ToActionResult() as OkResult;
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
}
