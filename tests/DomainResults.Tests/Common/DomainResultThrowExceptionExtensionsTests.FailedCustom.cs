using System;
using System.Diagnostics.CodeAnalysis;

using DomainResults.Common;
using DomainResults.Common.Exceptions;

using Xunit;

namespace DomainResults.Tests.Common;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public partial class DomainResult_Throw_Exception_Extensions_Tests
{
	[Fact]
	public void Failed_IDomainResult_Throws_Custom_Exception_On_Check()
	{
		var domainResult = IDomainResult.Failed("Bla");
		var exc = Assert.Throws<CustomException>(
			() => domainResult.ThrowIfNoSuccess<CustomException>("Error Message")
			);
		Assert.Equal("Error Message", exc.Message);
	}
	[Fact]
	public void Failed_DomainResultOfT_Throws_Custom_Exception_On_Check()
	{
		var domainResult = DomainResult.Failed<int>("Bla");
		var exc = Assert.Throws<CustomException>(
			() => domainResult.ThrowIfNoSuccess<int,CustomException>("Error Message")
			);
		Assert.Equal("Error Message", exc.Message);
	}
	[Fact]
	public void Failed_IDomainResultOfT_Throws_Custom_Exception_On_Check()
	{
		var domainResult = IDomainResult.Failed<int>("Bla");
		var exc = Assert.Throws<CustomException>(
			() => domainResult.ThrowIfNoSuccess<int,CustomException>("Error Message")
		);
		Assert.Equal("Error Message", exc.Message);
	}
	[Fact]
	public async void Failed_DomainResult_Task_Throws_Custom_Exception_On_Check()
	{
		var domainResult = DomainResult.FailedTask("Bla");
		var exc = await Assert.ThrowsAsync<CustomException>(
			() => domainResult.ThrowIfNoSuccess<CustomException>("Error Message")
		);
		Assert.Equal("Error Message", exc.Message);
	}
	[Fact]
	public async void Failed_IDomainResult_Task_Throws_Custom_Exception_On_Check()
	{
		var domainResult = IDomainResult.FailedTask("Bla");
		var exc = await Assert.ThrowsAsync<CustomException>(
			() => domainResult.ThrowIfNoSuccess<CustomException>("Error Message")
		);
		Assert.Equal("Error Message", exc.Message);
	}
	[Fact]
	public async void Failed_DomainResultOfT_Task_Throws_Custom_Exception_On_Check()
	{
		var domainResult = DomainResult.FailedTask<int>("Bla");
		var exc = await Assert.ThrowsAsync<CustomException>(
			() => domainResult.ThrowIfNoSuccess<int,CustomException>("Error Message")
			);
		Assert.Equal("Error Message", exc.Message);
	}
	[Fact]
	public async void Failed_IDomainResultOfT_Task_Throws_Custom_Exception_On_Check()
	{
		var domainResult = IDomainResult.FailedTask<int>("Bla");
		var exc = await Assert.ThrowsAsync<CustomException>(
			() => domainResult.ThrowIfNoSuccess<int,CustomException>("Error Message")
		);
		Assert.Equal("Error Message", exc.Message);
	}
	
	private class CustomException : Exception
	{
		public CustomException(string msg): base(msg){}
		public CustomException(): base(){}
	}
}
