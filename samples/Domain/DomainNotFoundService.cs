using DomainResults.Common;

namespace DomainResults.Examples.Domain;

public class DomainNotFoundService
{
	public IDomainResult GetNotFoundWithNoMessage()	=> DomainResult.NotFound();
	public IDomainResult GetNotFoundWithMessage()	=> DomainResult.NotFound("No, really not found");
	public IDomainResult GetNotFoundWithMessages()	=> DomainResult.NotFound(new[] { "No, really not found", "Searched everywhere" });

	public Task<IDomainResult> GetNotFoundWithNoMessageTask()=> DomainResult.NotFoundTask();
	public Task<IDomainResult> GetNotFoundWithMessageTask()	 => DomainResult.NotFoundTask("No, really not found");
	public Task<IDomainResult> GetNotFoundWithMessagesTask() => DomainResult.NotFoundTask(new[] { "No, really not found", "Searched everywhere" });

	public IDomainResult<int> GetNotFoundWithNoMessageWhenExpectedNumber()	=> DomainResult.NotFound<int>();
	public IDomainResult<int> GetNotFoundWithMessageWhenExpectedNumber()	=> DomainResult.NotFound<int>("No, really not found");
	public IDomainResult<int> GetNotFoundWithMessagesWhenExpectedNumber()	=> DomainResult.NotFound<int>(new[] { "No, really not found", "Searched everywhere" });

	public Task<IDomainResult<int>> GetNotFoundWithNoMessageWhenExpectedNumberTask()=> DomainResult.NotFoundTask<int>();
	public Task<IDomainResult<int>> GetNotFoundWithMessageWhenExpectedNumberTask()	=> DomainResult.NotFoundTask<int>("No, really not found");
	public Task<IDomainResult<int>> GetNotFoundWithMessagesWhenExpectedNumberTask()	=> DomainResult.NotFoundTask<int>(new[] { "No, really not found", "Searched everywhere" });

	public (int, IDomainResult) GetNotFoundWithNoMessageWhenExpectedNumberTuple()	=> IDomainResult.NotFound<int>();
	public (int, IDomainResult) GetNotFoundWithMessageWhenExpectedNumberTuple()		=> IDomainResult.NotFound<int>("No, really not found");
	public (int, IDomainResult) GetNotFoundWithMessagesWhenExpectedNumberTuple()	=> IDomainResult.NotFound<int>(new[] { "No, really not found", "Searched everywhere" });

	public Task<(int, IDomainResult)> GetNotFoundWithNoMessageWhenExpectedNumberTupleTask()	=> IDomainResult.NotFoundTask<int>();
	public Task<(int, IDomainResult)> GetNotFoundWithMessageWhenExpectedNumberTupleTask()	=> IDomainResult.NotFoundTask<int>("No, really not found");
	public Task<(int, IDomainResult)> GetNotFoundWithMessagesWhenExpectedNumberTupleTask()	=> IDomainResult.NotFoundTask<int>(new[] { "No, really not found", "Searched everywhere" });
}
