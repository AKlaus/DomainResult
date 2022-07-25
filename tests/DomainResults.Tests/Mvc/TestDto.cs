namespace DomainResults.Tests.Mvc;

internal record TestDto
{
	// ReSharper disable once UnusedAutoPropertyAccessor.Global
	// ReSharper disable once MemberCanBePrivate.Global
	public string Prop { get; }
	public TestDto(string prop) { Prop = prop; }
}
