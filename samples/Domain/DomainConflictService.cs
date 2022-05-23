using System.Threading.Tasks;

using DomainResults.Common;

namespace DomainResults.Examples.Domain;

public class DomainConflictService
{
	private const string Message = "Optimistic concurrency control: the record was just updated by someone else";
	
	public IDomainResult GetConflictWithNoMessage()	=> DomainResult.Conflict();
	public IDomainResult GetConflictWithMessage()	=> DomainResult.Conflict(Message);

	public Task<IDomainResult> GetConflictWithNoMessageTask()=> DomainResult.ConflictTask();
	public Task<IDomainResult> GetConflictWithMessageTask()	 => DomainResult.ConflictTask(Message);

	public IDomainResult<int> GetConflictWithNoMessageWhenExpectedNumber()	=> DomainResult.Conflict<int>();
	public IDomainResult<int> GetConflictWithMessageWhenExpectedNumber()	=> DomainResult.Conflict<int>(Message);

	public Task<IDomainResult<int>> GetConflictWithNoMessageWhenExpectedNumberTask()=> DomainResult.ConflictTask<int>();
	public Task<IDomainResult<int>> GetConflictWithMessageWhenExpectedNumberTask()	=> DomainResult.ConflictTask<int>(Message);

	public (int, IDomainResult) GetConflictWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.Conflict<int>();
	public (int, IDomainResult) GetConflictWithMessageWhenExpectedNumberTuple()		=> IDomainResult.Conflict<int>(Message);

	public Task<(int, IDomainResult)> GetConflictWithNoMessageWhenExpectedNumberTupleTask()	=> IDomainResult.ConflictTask<int>();
	public Task<(int, IDomainResult)> GetConflictWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.ConflictTask<int>(Message);
}
