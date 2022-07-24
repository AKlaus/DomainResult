using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DomainResults.Tests.Mvc;

[SuppressMessage("ReSharper", "InconsistentNaming")]
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
	
#if NET6_0_OR_GREATER	
	[Fact]
	public void DomainResultConverted_To_NoContent_of_IResult()
	{
		// GIVEN a successful domain result
		var domainRes = DomainResult.Success();

		// WHEN convert it to IResult
		var res = domainRes.ToResult();

		// THEN the response type is NoContent
		res.AssertNoContentResultType();
	}
#endif
	
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
	
#if NET6_0_OR_GREATER	
	[Fact]
	public async Task DomainResult_Task_Converted_To_NoContent_of_IResult()
	{
		// GIVEN a successful domain result
		var domainRes = DomainResult.SuccessTask();

		// WHEN convert it to IResult
		var res = await domainRes.ToResult();

		// THEN the response type is NoContent
		res.AssertNoContentResultType();
	}
#endif
}