namespace DomainResults.Common
{
	/// <summary>
	///		Status of the domain operation
	/// </summary>
	public enum DomainOperationStatus
	{
		/// <summary>
		///		Successful operation (gets converted to HTTP code 2xx on the API)
		/// </summary>
		Success,
		/// <summary>
		///		Entity not found (gets converted to HTTP code 404 on the API)
		/// </summary>
		NotFound,
		/// <summary>
		///		Failed operation (gets converted to HTTP code 400 on the API)
		/// </summary>
		Failed,
		/// <summary>
		///		Refused to authorize the operation (gets converted to HTTP code 403 on the API)
		/// </summary>
		Unauthorized,
		/// <summary>
		///		Conflict with the current state of the target resource (gets converted to HTTP code 409 on the API)
		/// </summary>
		Conflict,
		/// <summary>
		///		The requested entity is larger than limits defined by server (gets converted to HTTP code 413 on the API)
		/// </summary>
		PayloadTooLarge,
		/// <summary>
		///		External service call failed (gets converted to HTTP code 503 'Service Unavailable' on the API)
		/// </summary>
		CriticalDependencyError,
	}
}
