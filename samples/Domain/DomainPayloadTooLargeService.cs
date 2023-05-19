using System.Threading.Tasks;

using DomainResults.Common;

namespace DomainResults.Examples.Domain;

public class DomainPayloadTooLargeService
{
	private const string Message = "The request entity is larger than limits defined by server";
	
	public IDomainResult GetPayloadTooLargeWithNoMessage()	=> DomainResult.PayloadTooLarge();
	public IDomainResult GetPayloadTooLargeWithMessage()	=> DomainResult.PayloadTooLarge(Message);

	public Task<IDomainResult> GetPayloadTooLargeWithNoMessageTask()=> DomainResult.PayloadTooLargeTask();
	public Task<IDomainResult> GetPayloadTooLargeWithMessageTask()	 => DomainResult.PayloadTooLargeTask(Message);

	public IDomainResult<int> GetPayloadTooLargeWithNoMessageWhenExpectedNumber()	=> DomainResult.PayloadTooLarge<int>();
	public IDomainResult<int> GetPayloadTooLargeWithMessageWhenExpectedNumber()	=> DomainResult.PayloadTooLarge<int>(Message);

	public Task<IDomainResult<int>> GetPayloadTooLargeWithNoMessageWhenExpectedNumberTask()=> DomainResult.PayloadTooLargeTask<int>();
	public Task<IDomainResult<int>> GetPayloadTooLargeWithMessageWhenExpectedNumberTask()	=> DomainResult.PayloadTooLargeTask<int>(Message);

	public (int, IDomainResult) GetPayloadTooLargeWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.PayloadTooLarge<int>();
	public (int, IDomainResult) GetPayloadTooLargeWithMessageWhenExpectedNumberTuple()		=> IDomainResult.PayloadTooLarge<int>(Message);

	public Task<(int, IDomainResult)> GetPayloadTooLargeWithNoMessageWhenExpectedNumberTupleTask()	=> IDomainResult.PayloadTooLargeTask<int>();
	public Task<(int, IDomainResult)> GetPayloadTooLargeWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.PayloadTooLargeTask<int>(Message);
}
