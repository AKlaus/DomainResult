namespace DomainResults.Common
{
	/// <summary>
	///		Status of the domain operation
	/// </summary>
	public enum DomainOperationStatus
	{
		/// <summary>
		///		Successful operation (can be converted to HTTP code 2xx on the API)
		/// </summary>
		Success,
		/// <summary>
		///		Entity not found (can be converted to HTTP code 404 on the API)
		/// </summary>
		NotFound,
		/// <summary>
		///		Failed operation (can be converted to HTTP code 4xx on the API)
		/// </summary>
		Error
	}
}
