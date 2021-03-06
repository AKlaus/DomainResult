﻿namespace DomainResults.Mvc
{
	/// <summary>
	///		Conventions for returned HTTP codes and messages
	/// </summary>
	public static class ActionResultConventions
	{
		/// <summary>
		///		The HTTP code to return for client request error. Can be either 400 (default) or 422
		/// </summary>
		/// <remarks>
		///		Opinions: https://stackoverflow.com/a/52098667/968003, https://stackoverflow.com/a/20215807/968003
		/// </remarks>
		public static int ErrorHttpCode { get; set; } = 400;
		/// <summary>
		///		The title in the returned JSON accompanying the <see cref="ErrorHttpCode"/> response (HTTP code 4xx).
		///		The default value: "Bad Request"
		/// </summary>
		public static string ErrorProblemDetailsTitle { get; set; } = "Bad Request";

		/// <summary>
		///		The HTTP code to return when a record not found. The default value: 404
		/// </summary>
		public static int NotFoundHttpCode { get; set; } = 404;
		/// <summary>
		///		The title in the returned JSON accompanying the <see cref="NotFoundHttpCode"/> response (Not Found)
		///		The default value: "Not Found"
		/// </summary>
		public static string NotFoundProblemDetailsTitle { get; set; } = "Not Found";

		/// <summary>
		///		The HTTP code to return when a access is forbidden and returned 'unauthorized' status. The default value: 403
		/// </summary>
		public static int UnauthorizedHttpCode { get; set; } = 403;
		/// <summary>
		///		The title in the returned JSON accompanying the <see cref="UnauthorizedHttpCode"/> response (Forbidden)
		///		The default value: "Unauthorized access"
		/// </summary>
		public static string UnauthorizedProblemDetailsTitle { get; set; } = "Unauthorized access";
	}
}
