using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using DomainResults.Mvc.Helpers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Mvc;

/// <summary>
///     Generates URL for a controller method.
///     Sacrificing performance in favour of maintainability
/// </summary>
/// <example>
///     // For a controller method like
///     public object Get(int id) { return new { id = id }; }
///     // Return the action method URL with strongly typed checks:
///     Url.HttpRouteUrl<TestsApiController>(c => c.Get(1))
/// </example>
/// <remarks>
///		Taken from https://stackoverflow.com/a/44334632/968003
/// </remarks>
public static class GenericUrlActionHelper
{
    /// <summary>
    ///     Generates a fully qualified URL to an action method 
    /// </summary>
    public static string Action<TController>(this UrlHelper urlHelper, Expression<Action<TController>> action) where TController : ControllerBase
    {
        RouteValueDictionary rvd = InternalExpressionHelper.GetRouteValues(action);
        return urlHelper.Action(null, null, rvd);
    }

    public const string HttpAttributeRouteWebApiKey = "__RouteName";

    public static string HttpRouteUrl<TController>(this UrlHelper urlHelper, Expression<Action<TController>> expression) where TController : ControllerBase
    {
        var routeValues = expression.GetRouteValues();
        var httpRouteKey = System.Web.Http.Routing.HttpRoute.HttpRouteKey;
        if (!routeValues.ContainsKey(httpRouteKey))
        {
            routeValues.Add(httpRouteKey, true);
        }
        var url = string.Empty;
        if (routeValues.ContainsKey(HttpAttributeRouteWebApiKey))
        {
            var routeName = routeValues[HttpAttributeRouteWebApiKey] as string;
            routeValues.Remove(HttpAttributeRouteWebApiKey);
            routeValues.Remove("controller");
            routeValues.Remove("action");
            url = urlHelper.Link(routeName, routeValues);
        }
        else
        {
            var path = resolvePath(routeValues, expression);
            var root = GetRootPath(urlHelper);
            url = root + path;
        }
        return url;
    }

    private static string resolvePath<TController>(RouteValueDictionary routeValues, Expression<Action<TController>> expression) where TController : ControllerBase
    {
        var controllerName = routeValues["controller"] as string;
        var actionName = routeValues["action"] as string;
        routeValues.Remove("controller");
        routeValues.Remove("action");

        var method = expression.AsMethodCallExpression().Method;

        var configuration = System.Web.Http.GlobalConfiguration.Configuration;
        var apiDescription = configuration.Services.GetApiExplorer().ApiDescriptions
           .FirstOrDefault(c =>
               c.ActionDescriptor.ControllerDescriptor.ControllerType == typeof(TController)
               && c.ActionDescriptor.ControllerDescriptor.ControllerType.GetMethod(actionName) == method
               && c.ActionDescriptor.ActionName == actionName
           );

        var route = apiDescription.Route;
        var routeData = new HttpRouteData(route, new HttpRouteValueDictionary(routeValues));

        var request = new System.Net.Http.HttpRequestMessage();
        request.Properties[System.Web.Http.Hosting.HttpPropertyKeys.HttpConfigurationKey] = configuration;
        request.Properties[System.Web.Http.Hosting.HttpPropertyKeys.HttpRouteDataKey] = routeData;

        var virtualPathData = route.GetVirtualPath(request, routeValues);

        var path = virtualPathData.VirtualPath;

        return path;
    }

    private static string GetRootPath(UrlHelper urlHelper)
    {
        var request = urlHelper.ActionContext.HttpContext.Request;
        var scheme = request.Scheme;
        var server = request.Headers["Host"].ToString() ?? request.Host.Value;
        var host = string.Format("{0}://{1}", scheme, server);
        var root = host + ToAbsolute("~");
        return root;
    }

    static string ToAbsolute(string virtualPath)
    {
        return VirtualPathUtility.ToAbsolute(virtualPath);
    }
}
