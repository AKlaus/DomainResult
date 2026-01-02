using DomainResults.Common;

namespace DomainResults.Examples.Domain;

public class DomainContentTooLargeService
{
	private const string Message = "Additional message when the requested entity is larger than limits defined by server";
	
	public IDomainResult GetContentTooLargeWithNoMessage()	=> DomainResult.ContentTooLarge();
	public IDomainResult GetContentTooLargeWithMessage()	=> DomainResult.ContentTooLarge(Message);

	public Task<IDomainResult> GetContentTooLargeWithNoMessageTask()=> DomainResult.ContentTooLargeTask();
	public Task<IDomainResult> GetContentTooLargeWithMessageTask()	 => DomainResult.ContentTooLargeTask(Message);

	public IDomainResult<int> GetContentTooLargeWithNoMessageWhenExpectedNumber()	=> DomainResult.ContentTooLarge<int>();
	public IDomainResult<int> GetContentTooLargeWithMessageWhenExpectedNumber()	=> DomainResult.ContentTooLarge<int>(Message);

	public Task<IDomainResult<int>> GetContentTooLargeWithNoMessageWhenExpectedNumberTask()=> DomainResult.ContentTooLargeTask<int>();
	public Task<IDomainResult<int>> GetContentTooLargeWithMessageWhenExpectedNumberTask()	=> DomainResult.ContentTooLargeTask<int>(Message);

	public (int, IDomainResult) GetContentTooLargeWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.ContentTooLarge<int>();
	public (int, IDomainResult) GetContentTooLargeWithMessageWhenExpectedNumberTuple()		=> IDomainResult.ContentTooLarge<int>(Message);

	public Task<(int, IDomainResult)> GetContentTooLargeWithNoMessageWhenExpectedNumberTupleTask()	=> IDomainResult.ContentTooLargeTask<int>();
	public Task<(int, IDomainResult)> GetContentTooLargeWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.ContentTooLargeTask<int>(Message);
}
