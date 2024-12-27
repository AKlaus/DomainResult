using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Common.Exceptions;

using Xunit;

namespace DomainResults.Tests.Common;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public partial class DomainResult_Throw_Exception_Extensions_Tests
{
	[Fact]
	public void Successful_IDomainResult_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = IDomainResult.Success();
		domainResult.ThrowIfNoSuccess();
		// No exception thrown
		Assert.True(domainResult.IsSuccess);
	}
	[Fact]
	public void Successful_DomainResultOfT_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = DomainResult.Success(10);
		var value = domainResult.ThrowIfNoSuccess();
		
		Assert.True(domainResult.IsSuccess);
		Assert.Equal(10, value);
	}
	[Fact]
	public void Successful_IDomainResultOfT_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = IDomainResult.Success(10);
		var value = domainResult.ThrowIfNoSuccess();
		
		Assert.Equal(10, value);
	}
	[Fact]
	public async void Successful_IDomainResult_Task_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = IDomainResult.SuccessTask();
		await domainResult.ThrowIfNoSuccess();
		
		Assert.True((await domainResult).IsSuccess);
	}
	[Fact]
	public async void Successful_DomainResultOfT_Task_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = DomainResult.SuccessTask(10);
		var value = await domainResult.ThrowIfNoSuccess();
		
		Assert.True((await domainResult).IsSuccess);
		Assert.Equal(10, value);
	}
	[Fact]
	public async void Successful_IDomainResultOfT_Task_Doesnt_Throw_Exception_On_Check()
	{
		var domainResult = IDomainResult.SuccessTask(10);
		var value = await domainResult.ThrowIfNoSuccess();
		// No exception thrown
		Assert.Equal(10, value);
	}
}
