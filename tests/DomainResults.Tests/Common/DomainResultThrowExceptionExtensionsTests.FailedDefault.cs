using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Common.Exceptions;

using Xunit;

namespace DomainResults.Tests.Common;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public partial class DomainResult_Throw_Exception_Extensions_Tests
{
	[Fact]
	public void Failed_IDomainResult_Throws_Exception_On_Check()
	{
		var domainResult = IDomainResult.Failed("Bla");
		var exc = Assert.Throws<DomainResultException>(
			() => domainResult.ThrowIfNoSuccess("Error Message")
			);
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
	[Fact]
	public void Failed_DomainResultOfT_Throws_Exception_On_Check()
	{
		var domainResult = DomainResult.Failed<int>("Bla");
		var exc = Assert.Throws<DomainResultException>(
			() => domainResult.ThrowIfNoSuccess("Error Message")
			);
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
	[Fact]
	public void Failed_IDomainResultOfT_Throws_Exception_On_Check()
	{
		var domainResult = IDomainResult.Failed<int>("Bla");
		var exc = Assert.Throws<DomainResultException>(
			() => domainResult.ThrowIfNoSuccess("Error Message")
		);
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
	[Fact]
	public async void Failed_DomainResult_Task_Throws_Exception_On_Check()
	{
		var domainResult = DomainResult.FailedTask("Bla");
		var exc = await Assert.ThrowsAsync<DomainResultException>(
			() => domainResult.ThrowIfNoSuccess("Error Message")
		);
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
	[Fact]
	public async void Failed_IDomainResult_Task_Throws_Exception_On_Check()
	{
		var domainResult = IDomainResult.FailedTask("Bla");
		var exc = await Assert.ThrowsAsync<DomainResultException>(
			() => domainResult.ThrowIfNoSuccess("Error Message")
		);
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
	[Fact]
	public async void Failed_DomainResultOfT_Task_Throws_Exception_On_Check()
	{
		var domainResult = DomainResult.FailedTask<int>("Bla");
		var exc = await Assert.ThrowsAsync<DomainResultException>(
			() => domainResult.ThrowIfNoSuccess("Error Message")
			);
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
	[Fact]
	public async void Failed_IDomainResultOfT_Task_Throws_Exception_On_Check()
	{
		var domainResult = IDomainResult.FailedTask<int>("Bla");
		var exc = await Assert.ThrowsAsync<DomainResultException>(
			() => domainResult.ThrowIfNoSuccess("Error Message")
		);
		Assert.Equal("Error Message", exc.Message);
		Assert.Equal(DomainOperationStatus.Failed, exc.DomainResult.Status);
		Assert.Equal(["Bla"], exc.DomainResult.Errors);
	}
}
