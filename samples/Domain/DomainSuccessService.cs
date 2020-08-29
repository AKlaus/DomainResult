using System.Threading.Tasks;

using DomainResults.Domain;

namespace DomainResults.Examples.Domain
{
	public class DomainSuccessService
	{
		public IDomainResult		GetSuccess()					  => DomainResult.Success();
		public IDomainResult<int>	GetSuccessWithNumericValue()	  => DomainResult.Success(10);
		public (int, IDomainResult)	GetSuccessWithNumericValueTuple() => IDomainResult.Success(10);

		public Task<IDomainResult>		  GetSuccessTask()						=> DomainResult.SuccessTask();
		public Task<IDomainResult<int>>	  GetSuccessWithNumericValueTask()		=> DomainResult.SuccessTask(10);
		public Task<(int, IDomainResult)> GetSuccessWithNumericValueTupleTask()	=> IDomainResult.SuccessTask(10);
	}
}
