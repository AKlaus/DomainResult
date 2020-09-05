namespace DomainResults.Mvc
{
	/// <summary>
	///		Conventions for returned HTTP codes and messages
	/// </summary>
	public static class ActionResultConventions
	{
		/// <summary>
		///		The HTTP code to return for client request error. Can be either 400 or 422
		/// </summary>
		/// <remarks>
		///		Opinions: https://stackoverflow.com/a/52098667/968003, https://stackoverflow.com/a/20215807/968003
		/// </remarks>
		public static int ErrorHttpCode { get; set; } = 422;
		/// <summary>
		///		The title in the returned JSON accompanying the <see cref="ErrorHttpCode"/> response (HTTP code 4xx)
		/// </summary>
		public static string ErrorProblemDetailsTitle { get; set; } = "Bad Request";

		/// <summary>
		///		The HTTP code to return when a record not found. Should be 404
		/// </summary>
		public static int NotFoundHttpCode { get; set; } = 404;
		/// <summary>
		///		The title in the returned JSON accompanying the <see cref="NotFoundHttpCode"/> response (Not Found)
		/// </summary>
		public static string NotFoundProblemDetailsTitle { get; set; } = "Not Found";
	}
}
