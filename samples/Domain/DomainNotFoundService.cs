using System.Threading.Tasks;

using AK.DomainResults.Domain;

namespace AK.DomainResults.Examples.Domain
{
	public class DomainNotFoundService
	{
		public IDomainResult GetNotFoundWithNoMessage()	=> DomainResult.NotFound();
		public IDomainResult GetNotFoundWithMessage()	=> DomainResult.NotFound("No, really not found");
		public IDomainResult GetNotFoundWithMessages()	=> DomainResult.NotFound(new[] { "No, really not found", "Searched everwhere" });

		public Task<IDomainResult> GetNotFoundWithNoMessageTask()=> DomainResult.NotFoundTask();
		public Task<IDomainResult> GetNotFoundWithMessageTask()	 => DomainResult.NotFoundTask("No, really not found");
		public Task<IDomainResult> GetNotFoundWithMessagesTask() => DomainResult.NotFoundTask(new[] { "No, really not found", "Searched everwhere" });

		public IDomainResult<int> GetNotFoundWithNoMessageWhenExpectedNumber()	=> DomainResult.NotFound<int>();
		public IDomainResult<int> GetNotFoundWithMessageWhenExpectedNumber()	=> DomainResult.NotFound<int>("No, really not found");
		public IDomainResult<int> GetNotFoundWithMessagesWhenExpectedNumber()	=> DomainResult.NotFound<int>(new[] { "No, really not found", "Searched everwhere" });

		public Task<IDomainResult<int>> GetNotFoundWithNoMessageWhenExpectedNumberTask()=> DomainResult.NotFoundTask<int>();
		public Task<IDomainResult<int>> GetNotFoundWithMessageWhenExpectedNumberTask()	=> DomainResult.NotFoundTask<int>("No, really not found");
		public Task<IDomainResult<int>> GetNotFoundWithMessagesWhenExpectedNumberTask()	=> DomainResult.NotFoundTask<int>(new[] { "No, really not found", "Searched everwhere" });

		public (int, IDomainResult) GetNotFoundWithNoMessageWhenExpectedNumberTuple()	=> ValueResult.NotFound<int>();
		public (int, IDomainResult) GetNotFoundWithMessageWhenExpectedNumberTuple()		=> ValueResult.NotFound<int>("No, really not found");
		public (int, IDomainResult) GetNotFoundWithMessagesWhenExpectedNumberTuple()	=> ValueResult.NotFound<int>(new[] { "No, really not found", "Searched everwhere" });

		public Task<(int, IDomainResult)> GetNotFoundWithNoMessageWhenExpectedNumberTupleTask()	=> ValueResult.NotFoundTask<int>();
		public Task<(int, IDomainResult)> GetNotFoundWithMessageWhenExpectedNumberTupleTask()	=> ValueResult.NotFoundTask<int>("No, really not found");
		public Task<(int, IDomainResult)> GetNotFoundWithMessagesWhenExpectedNumberTupleTask()	=> ValueResult.NotFoundTask<int>(new[] { "No, really not found", "Searched everwhere" });
	}
}
