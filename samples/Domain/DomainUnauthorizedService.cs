using System.Threading.Tasks;

using DomainResults.Common;

namespace DomainResults.Examples.Domain;

public class DomainUnauthorizedService
{
	private const string Message = "No, no access";

	public IDomainResult GetUnauthorizedWithNoMessage()	=> DomainResult.Unauthorized();
	public IDomainResult GetUnauthorizedWithMessage()	=> DomainResult.Unauthorized(Message);

	public Task<IDomainResult> GetUnauthorizedWithNoMessageTask()=> DomainResult.UnauthorizedTask();
	public Task<IDomainResult> GetUnauthorizedWithMessageTask()	 => DomainResult.UnauthorizedTask(Message);

	public IDomainResult<int> GetUnauthorizedWithNoMessageWhenExpectedNumber()	=> DomainResult.Unauthorized<int>();
	public IDomainResult<int> GetUnauthorizedWithMessageWhenExpectedNumber()	=> DomainResult.Unauthorized<int>(Message);

	public Task<IDomainResult<int>> GetUnauthorizedWithNoMessageWhenExpectedNumberTask()=> DomainResult.UnauthorizedTask<int>();
	public Task<IDomainResult<int>> GetUnauthorizedWithMessageWhenExpectedNumberTask()	=> DomainResult.UnauthorizedTask<int>(Message);

	public (int, IDomainResult) GetUnauthorizedWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.Unauthorized<int>();
	public (int, IDomainResult) GetUnauthorizedWithMessageWhenExpectedNumberTuple()		=> IDomainResult.Unauthorized<int>(Message);

	public Task<(int, IDomainResult)> GetUnauthorizedWithNoMessageWhenExpectedNumberTupleTask()	=> IDomainResult.UnauthorizedTask<int>();
	public Task<(int, IDomainResult)> GetUnauthorizedWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.UnauthorizedTask<int>(Message);
}
