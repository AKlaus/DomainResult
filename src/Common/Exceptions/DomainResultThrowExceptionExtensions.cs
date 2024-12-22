using System.Threading.Tasks;

namespace DomainResults.Common.Exceptions
{
	/// <summary>
	///     Extension methods to throw exception if <see cref="DomainResult.IsSuccess"/> is <value>false</value>.
	/// </summary>
	public static class DomainResultThrowExceptionExtensions
	{
		/// <summary>
		///		Throw <see cref="DomainResultException"/> if <paramref name="domainResult"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResult"> The IDomainResult to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		public static void ThrowIfNoSuccess(this IDomainResult domainResult, string? errMsg = null) 
		{
			if (!domainResult.IsSuccess)
				throw new DomainResultException(domainResult, errMsg);
		}
		/// <summary>
		///		Throw <see cref="DomainResultException"/> if <paramref name="domainResult"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResult"> The IDomainResult to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		public static void ThrowIfNoSuccess<T>(this IDomainResult<T> domainResult, string? errMsg = null)
		{
			if (!domainResult.IsSuccess)
				throw new DomainResultException(domainResult, errMsg);
		}
		
		/// <summary>
		///		Throw <see cref="DomainResultException"/> if <paramref name="domainResultTask"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResultTask"> The IDomainResult to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		public static async Task ThrowIfNoSuccess(this Task<IDomainResult> domainResultTask, string? errMsg = null)
		{		
			var domainResult = await domainResultTask.ConfigureAwait(true);
			if (!domainResult.IsSuccess)
				throw new DomainResultException(domainResult, errMsg);
		}
		/// <summary>
		///		Throw <see cref="DomainResultException"/> if <paramref name="domainResultTask"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResultTask"> The IDomainResult to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		public static async Task ThrowIfNoSuccess<T>(this Task<IDomainResult<T>> domainResultTask, string? errMsg = null)
		{
			var domainResult = await domainResultTask.ConfigureAwait(true);
			if (!domainResult.IsSuccess)
				throw new DomainResultException(domainResult, errMsg);
		}
	}
}
