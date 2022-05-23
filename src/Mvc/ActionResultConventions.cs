using DomainResults.Common;

namespace DomainResults.Mvc;

/// <summary>
///		Conventions for returned HTTP codes and messages
/// </summary>
public static class ActionResultConventions
{
	/// <summary>
	///		The HTTP code to return for client request error (<see cref="DomainOperationStatus.Failed"/> status). Can be either 400 (default) or 422
	/// </summary>
	/// <remarks>
	///		Opinions: https://stackoverflow.com/a/52098667/968003, https://stackoverflow.com/a/20215807/968003
	/// </remarks>
	public static int FailedHttpCode { get; set; } = 400;
	/// <summary>
	///		The title in the returned JSON accompanying the <see cref="FailedHttpCode"/> response (HTTP code 4xx).
	///		The default value: "Bad Request"
	/// </summary>
	public static string FailedProblemDetailsTitle { get; set; } = "Bad Request";

	/// <summary>
	///		The HTTP code to return when a record not found (<see cref="DomainOperationStatus.NotFound"/> status). The default value: 404
	/// </summary>
	public static int NotFoundHttpCode { get; set; } = 404;
	/// <summary>
	///		The title in the returned JSON accompanying the <see cref="NotFoundHttpCode"/> response (Not Found)
	///		The default value: "Not Found"
	/// </summary>
	public static string NotFoundProblemDetailsTitle { get; set; } = "Not Found";

	/// <summary>
	///		The HTTP code to return when a access is forbidden (<see cref="DomainOperationStatus.Unauthorized"/> status). The default value: 403
	/// </summary>
	public static int UnauthorizedHttpCode { get; set; } = 403;
	/// <summary>
	///		The title in the returned JSON accompanying the <see cref="UnauthorizedHttpCode"/> response (Forbidden)
	///		The default value: "Unauthorized access"
	/// </summary>
	public static string UnauthorizedProblemDetailsTitle { get; set; } = "Unauthorized access";

	/// <summary>
	///		The HTTP code to return when failed due to a conflict with the current state of the target resource (<see cref="DomainOperationStatus.Conflict"/> status). The default value: 409
	/// </summary>
	public static int ConflictHttpCode { get; set; } = 409;
	/// <summary>
	///		The title in the returned JSON accompanying the <see cref="ConflictHttpCode"/> response (Conflict)
	///		The default value: "Conflict with the current state of the target resource"
	/// </summary>
	public static string ConflictProblemDetailsTitle { get; set; } = "Conflict with the current state of the target resource";

	/// <summary>
	///		The HTTP code to return when an external service call failed (<see cref="DomainOperationStatus.CriticalDependencyError"/> status). The default value: 503
	/// </summary>
	public static int CriticalDependencyErrorHttpCode { get; set; } = 503;
	/// <summary>
	///		The title in the returned JSON accompanying the <see cref="CriticalDependencyErrorHttpCode"/> response (Service Unavailable)
	///		The default value: "External service unavailable"
	/// </summary>
	public static string CriticalDependencyErrorProblemDetailsTitle { get; set; } = "External service unavailable";
}
