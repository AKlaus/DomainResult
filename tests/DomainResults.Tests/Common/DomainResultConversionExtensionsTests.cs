using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using DomainResults.Common;

using Xunit;

namespace DomainResults.Tests.Common;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class DomainResult_Conversion_Extensions_Tests
{
	#region Tests of converting IDomainResult to IDomainResult<T>
	
	[Fact]
	public void Successful_IDomainResult_Converts_To_IDomainResultOfT()
	{
		var domainResult = DomainResult.Success();
		var domainResultOfT = domainResult.To<int>();
		
		Assert.True(domainResultOfT.IsSuccess);
		Assert.Equal(0, domainResultOfT.Value);
	}
	
	[Fact]
	public void Errored_IDomainResult_Converts_To_IDomainResultOfT()
	{
		var domainResult = DomainResult.Failed("Bla");
		var domainResultOfT = domainResult.To<int>();
		
		Assert.False(domainResultOfT.IsSuccess);
		Assert.Equal(0, domainResultOfT.Value);
		Assert.Equal("Bla", domainResultOfT.Errors.Single());
	}
	
	[Fact]
	public async Task Successful_IDomainResult_Task_Converts_To_IDomainResultOfT_Task()
	{
		var domainResult = DomainResult.SuccessTask();
		var domainResultOfT = await domainResult.To<int>();
		
		Assert.True(domainResultOfT.IsSuccess);
		Assert.Equal(0, domainResultOfT.Value);
	}
	
	[Fact]
	public async Task Errored_IDomainResult_Task_Converts_To_IDomainResultOfT_Task()
	{
		var domainResult = DomainResult.FailedTask("Bla");
		var domainResultOfT = await domainResult.To<int>();
		
		Assert.False(domainResultOfT.IsSuccess);
		Assert.Equal(0, domainResultOfT.Value);
		Assert.Equal("Bla", domainResultOfT.Errors.Single());
	}
	#endregion
	
	#region Tests of converting IDomainResult<T> to IDomainResult<V>
	
	[Fact]
	public void Successful_IDomainResultOfT_Converts_To_IDomainResultOfV()
	{
		var domainResult = DomainResult.Success(10);
		var domainResultOfT = domainResult.To<char>();
		
		Assert.True(domainResultOfT.IsSuccess);
		Assert.Equal('\0' /* aka `default(char)` */, domainResultOfT.Value);
	}
	
	[Fact]
	public void Errored_IDomainResultOfT_Converts_To_IDomainResultOfV()
	{
		var domainResult = DomainResult.Failed<int>("Bla");
		var domainResultOfT = domainResult.To<char>();
		
		Assert.False(domainResultOfT.IsSuccess);
		Assert.Equal('\0' /* aka `default(char)` */, domainResultOfT.Value);
		Assert.Equal("Bla", domainResultOfT.Errors.Single());
	}
	
	[Fact]
	public async Task Successful_IDomainResultOfT_Task_Converts_To_IDomainResultOfV_Task()
	{
		var domainResult = DomainResult.SuccessTask(10);
		var domainResultOfT = await domainResult.To<int, char>();
		
		Assert.True(domainResultOfT.IsSuccess);
		Assert.Equal('\0' /* aka `default(char)` */, domainResultOfT.Value);
	}
	
	[Fact]
	public async Task Errored_IDomainResultOfT_Task_Converts_To_IDomainResultOfV_Task()
	{
		var domainResult = DomainResult.FailedTask<int>("Bla");
		var domainResultOfT = await domainResult.To<int,char>();
		
		Assert.False(domainResultOfT.IsSuccess);
		Assert.Equal('\0' /* aka `default(char)` */, domainResultOfT.Value);
		Assert.Equal("Bla", domainResultOfT.Errors.Single());
	}
	#endregion
	
	#region Tests of converting (TValue, IDomainResult) tuple to IDomainResult<T>
	
	[Fact]
	public void IDomainResult_and_Value_Tuple_Implicitly_Converted_To_IDomainResultOfT()
	{
		var domainResult = DomainResult.Failed("Bla");
		DomainResult<int> domainResultOfT = (0 /* aka `default` */, domainResult);
		
		Assert.False(domainResultOfT.IsSuccess);
		Assert.Equal("Bla", domainResultOfT.Errors.Single());
		Assert.Equal(0, domainResultOfT.Value);
	}
	#endregion
}
