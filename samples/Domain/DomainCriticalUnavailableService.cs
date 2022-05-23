using System.Threading.Tasks;

using DomainResults.Common;

namespace DomainResults.Examples.Domain;

public class DomainCriticalUnavailableService
{
	public IDomainResult GetCriticalDependencyErrorWithNoMessage()	=> DomainResult.CriticalDependencyError();
	public IDomainResult GetCriticalDependencyErrorWithMessage()	=> DomainResult.CriticalDependencyError("Third-party service failed");

	public Task<IDomainResult> GetCriticalDependencyErrorWithNoMessageTask()=> DomainResult.CriticalDependencyErrorTask();
	public Task<IDomainResult> GetCriticalDependencyErrorWithMessageTask()	=> DomainResult.CriticalDependencyErrorTask("Third-party service failed");

	public IDomainResult<int> GetCriticalDependencyErrorWithNoMessageWhenExpectedNumber()	=> DomainResult.CriticalDependencyError<int>();
	public IDomainResult<int> GetCriticalDependencyErrorWithMessageWhenExpectedNumber()		=> DomainResult.CriticalDependencyError<int>("Third-party service failed");

	public Task<IDomainResult<int>> GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTask()	=> DomainResult.CriticalDependencyErrorTask<int>();
	public Task<IDomainResult<int>> GetCriticalDependencyErrorWithMessageWhenExpectedNumberTask()	=> DomainResult.CriticalDependencyErrorTask<int>("Third-party service failed");

	public (int, IDomainResult) GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.CriticalDependencyError<int>();
	public (int, IDomainResult) GetCriticalDependencyErrorWithMessageWhenExpectedNumberTuple()		=> IDomainResult.CriticalDependencyError<int>("Third-party service failed");

	public Task<(int, IDomainResult)> GetCriticalDependencyErrorWithNoMessageWhenExpectedNumberTupleTask()	=> IDomainResult.CriticalDependencyErrorTask<int>();
	public Task<(int, IDomainResult)> GetCriticalDependencyErrorWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.CriticalDependencyErrorTask<int>("Third-party service failed");
}
