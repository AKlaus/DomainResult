using System.Threading.Tasks;

using AK.DomainResults.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class DomainResult_To_NoContentResult_Tests
	{
		[Fact]
		public void Successful_Result_NoContent()
		{
			// GIVEN a successful domain result
			var domainRes = DomainResult.Success();

			// WHEN convert it to ActionResult
			var actionRes = domainRes.ToActionResult();

			// THEN the response type is correct
			Assert.IsType<NoContentResult>(actionRes);
		}

		[Fact]
		public async Task Successful_Result_Task_NoContent()
		{
			// GIVEN a successful domain result
			var domainRes = DomainResult.SuccessTask();

			// WHEN convert it to ActionResult
			var actionRes = await domainRes.ToActionResult();

			// THEN the response type is correct
			Assert.IsType<NoContentResult>(actionRes);
		}
	}
}
