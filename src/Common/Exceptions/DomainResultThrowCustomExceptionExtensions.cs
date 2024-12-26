using System;
using System.Threading.Tasks;

namespace DomainResults.Common.Exceptions
{
	/// <summary>
	///     Extension methods to throw custom exceptions if <see cref="DomainResult.IsSuccess"/> is <value>false</value>.
	/// </summary>
	public static class DomainResultThrowCustomExceptionExtensions
	{
		/// <summary>
		///		Throw <typeparamref name="TE"/> if <paramref name="domainResult"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResult"> The IDomainResult to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <typeparam name="TE">The exception type to throw if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </typeparam>
		public static void ThrowIfNoSuccess<TE>(this IDomainResult domainResult, string? errMsg = null) where TE: Exception, new()
		{
			if (domainResult.IsSuccess) 
				return;
			try { throw (TE)Activator.CreateInstance(typeof(TE), errMsg); }
			catch (MissingMethodException) { throw new TE(); }
		}
		
		///  <summary>
		/// 		Throw <typeparamref name="TE"/> if <paramref name="domainResult"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		///  </summary>
		///  <param name="domainResult"> The IDomainResult to check </param>
		///  <param name="errMsg"> The error message </param>
		///  <returns> The value, if the <see cref="DomainResult.IsSuccess"/> is <value>true</value> </returns>
		///  <typeparam name="TE">The exception type to throw if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </typeparam>
		///  <typeparam name="T"> The value type of successful <paramref name="domainResult"/> </typeparam>
		public static T ThrowIfNoSuccess<T, TE>(this IDomainResult<T> domainResult, string? errMsg = null) where TE: Exception, new()
		{
			if (domainResult.IsSuccess)
				return domainResult.Value;
			try { throw (TE)Activator.CreateInstance(typeof(TE), errMsg); }
			catch (MissingMethodException) { throw new TE(); }
		}
		/// <summary>
		/// 	Throw <typeparamref name="TE"/> if <paramref name="domainResult"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResult"> The <see cref="System.ValueTuple{T,IDomainResult}"/> to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <returns> The value, if the <see cref="DomainResult.IsSuccess"/> is <value>true</value> </returns>
		/// <typeparam name="TE">The exception type to throw if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </typeparam>
		/// <typeparam name="T"> The value type of successful <paramref name="domainResult"/> </typeparam>
		public static T ThrowIfNoSuccess<T, TE>(this (T result, IDomainResult status) domainResult, string? errMsg = null) where TE: Exception, new()
		{
			var (result, status) = domainResult;
			if (status.IsSuccess)
				return result;
			try { throw (TE)Activator.CreateInstance(typeof(TE), errMsg); }
			catch (MissingMethodException) { throw new TE(); }
		}
		
		/// <summary>
		/// 	Throw <typeparamref name="TE"/> if <paramref name="domainResultTask"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResultTask"> The <see cref="System.Threading.Tasks.Task{IDomainResult}"/> to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <exception cref="DomainResultException"> The thrown exception if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </exception>
		/// <typeparam name="TE">The exception type to throw if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </typeparam>
		public static async Task ThrowIfNoSuccess<TE>(this Task<IDomainResult> domainResultTask, string? errMsg = null) where TE: Exception, new()
		{		
			var domainResult = await domainResultTask.ConfigureAwait(true);
			if (domainResult.IsSuccess)
				return;
			try { throw (TE)Activator.CreateInstance(typeof(TE), errMsg); }
			catch (MissingMethodException) { throw new TE(); }
		}
		/// <summary>
		///		Throw <typeparamref name="TE"/> if <paramref name="domainResultTask"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResultTask"> The <see cref="System.Threading.Tasks.Task{IDomainResult}"/> to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <returns> The value, if the <see cref="DomainResult.IsSuccess"/> is <value>true</value> </returns>
		/// <typeparam name="TE">The exception type to throw if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </typeparam>
		/// <typeparam name="T"> The value type of successful <paramref name="domainResultTask"/> </typeparam>
		public static async Task<T> ThrowIfNoSuccess<T,TE>(this Task<IDomainResult<T>> domainResultTask, string? errMsg = null) where TE: Exception, new()
		{
			var domainResult = await domainResultTask.ConfigureAwait(true);
			if (domainResult.IsSuccess)
				return domainResult.Value;
			try { throw (TE)Activator.CreateInstance(typeof(TE), errMsg); }
			catch (MissingMethodException) { throw new TE(); }
		}
		/// <summary>
		///		Throw <typeparamref name="TE"/> if <paramref name="domainResultTask"/>'s <see cref="DomainResult.IsSuccess"/> is <value>false</value> 
		/// </summary>
		/// <param name="domainResultTask"> The <see cref="System.Threading.Tasks.Task{IDomainResult}"/> to check </param>
		/// <param name="errMsg"> The error message </param>
		/// <returns> The value, if the <see cref="DomainResult.IsSuccess"/> is <value>true</value> </returns>
		/// <typeparam name="TE">The exception type to throw if the <see cref="DomainResult.IsSuccess"/> is <value>false</value> </typeparam>
		/// <typeparam name="T"> The value type of successful <paramref name="domainResultTask"/> </typeparam>
		public static async Task<T> ThrowIfNoSuccess<T,TE>(this Task<(T result, IDomainResult status)> domainResultTask, string? errMsg = null) where TE: Exception, new()
		{
			var (result, status) = await domainResultTask.ConfigureAwait(true);
			if (status.IsSuccess)
				return result;
			try { throw (TE)Activator.CreateInstance(typeof(TE), errMsg); }
			catch (MissingMethodException) { throw new TE(); }
		}
	}
}