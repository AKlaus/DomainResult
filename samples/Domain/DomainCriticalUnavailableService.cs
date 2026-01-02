using DomainResults.Common;

namespace DomainResults.Examples.Domain;

public class DomainCriticalUnavailableService
{
	private const string Message = "Third-party service failed";

	public IDomainResult GetCriticalDependencyErrorWithNoMessage()	=> DomainResult.CriticalDependencyError();
	public IDomainResult GetCriticalDependencyErrorWithMessage()	=> DomainResult.CriticalDependencyError(Message);

	public Task<IDomainResult> GetCriticalDependencyErrorWithNoMessageTask()=> DomainResult.CriticalDependencyErrorTask();
	public Task<IDomainResult> GetCriticalDependencyErrorWithMessageTask()	=> DomainResult.CriticalDependencyErrorTask(Message);

	public IDomainResult<int> GetCriticalDependencyErrorWithNoMessageWhenExpectedNumber()	=> DomainResult.CriticalDependencyError<int>();
	public IDomainResult<int> GetCriticalDependencyErrorWithMessageWhenExpectedNumber()		=> DomainResult.CriticalDependencyError<int>(Message);

	public Task<IDomainResult<int>> GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTask()	=> DomainResult.CriticalDependencyErrorTask<int>();
	public Task<IDomainResult<int>> GetCriticalDependencyErrorWithMessageWhenExpectedNumberTask()	=> DomainResult.CriticalDependencyErrorTask<int>(Message);

	public (int, IDomainResult) GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.CriticalDependencyError<int>();
	public (int, IDomainResult) GetCriticalDependencyErrorWithMessageWhenExpectedNumberTuple()		=> IDomainResult.CriticalDependencyError<int>(Message);

	public Task<(int, IDomainResult)> GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTupleTask()	=> IDomainResult.CriticalDependencyErrorTask<int>();
	public Task<(int, IDomainResult)> GetCriticalDependencyErrorWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.CriticalDependencyErrorTask<int>(Message);
}
