using System.Threading.Tasks;

using DomainResults.Common;

namespace DomainResults.Examples.Domain
{
	public class DomainUnauthorizedService
	{
		public IDomainResult GetUnauthorizedWithNoMessage()	=> DomainResult.Unauthorized();
		public IDomainResult GetUnauthorizedWithMessage()	=> DomainResult.Unauthorized("No, no access");

		public Task<IDomainResult> GetUnauthorizedWithNoMessageTask()=> DomainResult.UnauthorizedTask();
		public Task<IDomainResult> GetUnauthorizedWithMessageTask()	 => DomainResult.UnauthorizedTask("No, no access");

		public IDomainResult<int> GetUnauthorizedWithNoMessageWhenExpectedNumber()	=> DomainResult.Unauthorized<int>();
		public IDomainResult<int> GetUnauthorizedWithMessageWhenExpectedNumber()	=> DomainResult.Unauthorized<int>("No, no access");

		public Task<IDomainResult<int>> GetUnauthorizedWithNoMessageWhenExpectedNumberTask()=> DomainResult.UnauthorizedTask<int>();
		public Task<IDomainResult<int>> GetUnauthorizedWithMessageWhenExpectedNumberTask()	=> DomainResult.UnauthorizedTask<int>("No, no access");

		public (int, IDomainResult) GetUnauthorizedWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.Unauthorized<int>();
		public (int, IDomainResult) GetUnauthorizedWithMessageWhenExpectedNumberTuple()		=> IDomainResult.Unauthorized<int>("No, no access");

		public Task<(int, IDomainResult)> GetUnauthorizedWithNoMessageWhenExpectedNumberTupleTask()	=> IDomainResult.UnauthorizedTask<int>();
		public Task<(int, IDomainResult)> GetUnauthorizedWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.UnauthorizedTask<int>("No, no access");
	}
}
