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
		/// <returns> The value, if the <see cref="DomainResult.IsSuccess"/> is <value>true</value> </returns>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		public static T ThrowIfNoSuccess<T>(this IDomainResult<T> domainResult, string? errMsg = null)
		{
			if (!domainResult.IsSuccess)
				throw new DomainResultException(domainResult, errMsg);
			return domainResult.Value;
		}
		/// <summary>
		///		Throw <see cref="DomainResultException"/> if <paramref name="domainResult"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResult"> The <see cref="System.ValueTuple{T,IDomainResult}"/> to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <returns> The value, if the <see cref="DomainResult.IsSuccess"/> is <value>true</value> </returns>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		public static T ThrowIfNoSuccess<T>(this (T result, IDomainResult status) domainResult, string? errMsg = null)
		{
			var (result, status) = domainResult;
			if (!status.IsSuccess)
				throw new DomainResultException(status, errMsg);
			return result;
		}
		
		/// <summary>
		///		Throw <see cref="DomainResultException"/> if <paramref name="domainResultTask"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResultTask"> The <see cref="System.Threading.Tasks.Task{IDomainResult}"/> to check </param>
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
		/// <param name="domainResultTask"> The <see cref="System.Threading.Tasks.Task{IDomainResult}"/> to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <returns> The value, if the <see cref="DomainResult.IsSuccess"/> is <value>true</value> </returns>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		public static async Task<T> ThrowIfNoSuccess<T>(this Task<IDomainResult<T>> domainResultTask, string? errMsg = null)
		{
			var domainResult = await domainResultTask.ConfigureAwait(true);
			if (!domainResult.IsSuccess)
				throw new DomainResultException(domainResult, errMsg);
			return domainResult.Value;
		}
		/// <summary>
		///		Throw <see cref="DomainResultException"/> if <paramref name="domainResultTask"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResultTask"> The <see cref="System.Threading.Tasks.Task{IDomainResult}"/> to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <returns> The value, if the <see cref="DomainResult.IsSuccess"/> is <value>true</value> </returns>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		public static async Task<T> ThrowIfNoSuccess<T>(this Task<(T result, IDomainResult status)> domainResultTask, string? errMsg = null)
		{
			var (result, status) = await domainResultTask.ConfigureAwait(true);
			if (!status.IsSuccess)
				throw new DomainResultException(status, errMsg);
			return result;
		}
	}
}
