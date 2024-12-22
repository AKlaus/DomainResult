using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Common.Exceptions;

using Xunit;

namespace DomainResults.Tests.Common;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class DomainResult_Throw_Exception_Extensions_Tests
{
	[Fact]
	public void Successful_IDomainResult_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = DomainResult.Success();
		domainResult.ThrowIfNoSuccess();
		// No exception thrown
		Assert.True(domainResult.IsSuccess);
	}
	[Fact]
	public void Successful_IDomainResultOfT_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = DomainResult.Success(10);
		domainResult.ThrowIfNoSuccess();
		// No exception thrown
		Assert.True(domainResult.IsSuccess);
	}
	[Fact]
	public async void Successful_IDomainResult_Task_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = DomainResult.SuccessTask();
		domainResult.ThrowIfNoSuccess();
		// No exception thrown
		Assert.True((await domainResult).IsSuccess);
	}
	[Fact]
	public async void Successful_IDomainResultOfT_Task_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = DomainResult.SuccessTask(10);
		domainResult.ThrowIfNoSuccess();
		// No exception thrown
		Assert.True((await domainResult).IsSuccess);
	}
	
	[Fact]
	public void Failed_IDomainResult_Throws_Exception_On_Check()
	{
		var domainResult = DomainResult.Failed("Bla");
		var exc = Assert.Throws<DomainResultException>(() => domainResult.ThrowIfNoSuccess("Error Message"));
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
	[Fact]
	public void Failed_IDomainResultOfT_Throws_Exception_On_Check()
	{
		var domainResult = DomainResult.Failed<int>("Bla");
		var exc = Assert.Throws<DomainResultException>(() => domainResult.ThrowIfNoSuccess("Error Message"));
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
	[Fact]
	public void Failed_IDomainResultOfT_Task_Throws_Exception_On_Check()
	{
		var domainResult = DomainResult.FailedTask<int>("Bla");
		var exc = Assert.Throws<DomainResultException>(() => domainResult.ThrowIfNoSuccess("Error Message"));
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
}
