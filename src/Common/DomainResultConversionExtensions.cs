namespace DomainResults.Common;

/// <summary>
///     Extension methods to convert between compatible types
/// </summary>
public static class DomainResultConversionExtensions
{
	/// <summary>
	///     Convert to <see cref="IDomainResult{T}" /> (the domain operation result with a returned value)
	/// </summary>
	/// <typeparam name="T"> Value type of returned by the domain operation </typeparam>
	public static IDomainResult<T> To<T>(this IDomainResult domainResult)
	{
		return DomainResult<T>.From(domainResult);
	}

	/// <summary>
	///     Convert to a <see cref="Task" /> of <see cref="IDomainResult{T}" /> (the domain operation result with a returned value)
	/// </summary>
	/// <typeparam name="T"> Value type of returned by the domain operation </typeparam>
	public static async Task<IDomainResult<T>> To<T>(this Task<IDomainResult> domainResult)
	{
		return DomainResult<T>.From(await domainResult);
	}
		
	/// <summary>
	/// 	Convert to a <see cref="IDomainResult{T}" /> with a new value type <typeparamref name="TNew"/>
	/// </summary>
	/// <typeparam name="TOld"> The old value type (converting from) </typeparam>
	/// <typeparam name="TNew"> The new value type (converting to) </typeparam>
	public static IDomainResult<TNew> To<TOld,TNew>(this IDomainResult<TOld> domainResult)
	{
		return DomainResult<TNew>.From(domainResult);
	}

	/// <summary>
	/// 	Convert to a <see cref="Task"/> of <see cref="IDomainResult{T}" /> with a new value type <typeparamref name="TNew"/>
	/// </summary>
	/// <typeparam name="TOld"> The old value type (converting from) </typeparam>
	/// <typeparam name="TNew"> The new value type (converting to) </typeparam>
	public static async Task<IDomainResult<TNew>> To<TOld,TNew>(this Task<IDomainResult<TOld>> domainResult)
	{
		return DomainResult<TNew>.From(await domainResult);
	}
}