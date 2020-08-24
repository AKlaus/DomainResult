using System.Threading.Tasks;

using AK.DomainResults.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class DomainResultToNoContentResultTests
	{
		[Fact]
		public void SuccessfulResultNoContent()
		{
			// GIVEN a successful domain result
			var domainRes = DomainResult.Success();

			// WHEN convert it to ActionResult
			var actionRes = domainRes.ToActionResult();

			// THEN the response type is correct
			Assert.IsType<NoContentResult>(actionRes);
		}

		[Fact]
		public async Task SuccessfulResultTaskNoContent()
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
