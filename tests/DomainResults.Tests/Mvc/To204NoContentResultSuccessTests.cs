using System.Threading.Tasks;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc
{
	public class To_204_NoContentResult_Success_Tests
	{
		[Fact]
		public void DomainResultConverted_To_NoContent()
		{
			// GIVEN a successful domain result
			var domainRes = DomainResult.Success();

			// WHEN convert it to ActionResult
			var actionRes = domainRes.ToActionResult();

			// THEN the response type is correct
			Assert.IsType<NoContentResult>(actionRes);
		}

		[Fact]
		public async Task DomainResult_Task_Converted_To_NoContent()
		{
			// GIVEN a successful domain result
			var domainRes = DomainResult.SuccessTask();

			// WHEN convert it to ActionResult
			var actionRes = await domainRes.ToActionResult();

			// THEN the response type is correct
			Assert.IsType<NoContentResult>(actionRes);
		}

#if !NETCOREAPP2_0
		[Fact]
		public void DomainResultConverted_To_NoContentOfT()
		{
			// GIVEN a successful domain result
			var domainRes = DomainResult.Success();

			// WHEN convert it to ActionResult
			var actionRes = domainRes.ToActionResultOfT();

			// THEN the response type is correct
			Assert.IsType<NoContentResult>(actionRes.Result);
		}

		[Fact]
		public async Task DomainResult_Task_Converted_To_NoContentOfT()
		{
			// GIVEN a successful domain result
			var domainRes = DomainResult.SuccessTask();

			// WHEN convert it to ActionResult
			var actionRes = await domainRes.ToActionResultOfT();

			// THEN the response type is correct
			Assert.IsType<NoContentResult>(actionRes.Result);
		}
#endif
	}
}
