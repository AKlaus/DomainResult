namespace DomainResults.Common.Exceptions;

public class DomainResultException : Exception
{
	public DomainResult DomainResult { get; }
	
	public DomainResultException(IDomainResultBase domainResult, string? message = null) : base(message)
	{
		DomainResult = new DomainResult(domainResult.Status, domainResult.Errors);
	}
}
