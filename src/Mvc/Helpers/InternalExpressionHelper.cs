using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainResults.Mvc.Helpers
{
	static class InternalExpressionHelper
    {
        /// <summary>
        ///     Extract route values from strongly typed expression
        /// </summary>
        public static RouteValueDictionary GetRouteValues<TController>(this Expression<Action<TController>> expression, RouteValueDictionary? routeValues = null)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            if (routeValues == null) 
                routeValues = new RouteValueDictionary();

            var controllerType = EnsureController<TController>();

            routeValues["controller"] = EnsureControllerName(controllerType);

            var methodCallExpression = AsMethodCallExpression(expression);

            routeValues["action"] = methodCallExpression.Method.Name;

            // Add parameter values from expression to dictionary
            var parameters = BuildParameterValuesFromExpression(methodCallExpression);
            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> parameter in parameters)
                {
                    routeValues.Add(parameter.Key, parameter.Value);
                }
            }

            // Try to extract route attribute name if present on an api controller.
            if (typeof(ControllerBase).IsAssignableFrom(controllerType))
            {
                var routeAttribute = methodCallExpression.Method.GetCustomAttribute<RouteAttribute>(false);
                if (routeAttribute != null && routeAttribute.Name != null)
                {
                    routeValues[GenericUrlActionHelper.HttpAttributeRouteWebApiKey] = routeAttribute.Name;
                }
            }

            return routeValues;
        }

        private static string EnsureControllerName(Type controllerType)
        {
            var controllerName = controllerType.Name;
            if (!controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Action target must end in controller", "action");
            }
            controllerName = controllerName.Remove(controllerName.Length - 10, 10);
            if (controllerName.Length == 0)
            {
                throw new ArgumentException("Action cannot route to controller", "action");
            }
            return controllerName;
        }

        internal static MethodCallExpression AsMethodCallExpression<TController>(this Expression<Action<TController>> expression)
        {
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression == null)
                throw new InvalidOperationException("Expression must be a method call.");

            if (methodCallExpression.Object != expression.Parameters[0])
                throw new InvalidOperationException("Method call must target lambda argument.");

            return methodCallExpression;
        }

        private static Type EnsureController<TController>()
        {
            var controllerType = typeof(TController);

            bool isController = controllerType != null
                   && controllerType.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                   && !controllerType.IsAbstract
                   && (
                        typeof(ControllerBase).IsAssignableFrom(controllerType)
                        || typeof(ControllerBase).IsAssignableFrom(controllerType)
                      );

            if (!isController)
                throw new InvalidOperationException("Action target is an invalid controller.");

            return controllerType!;
        }

        private static RouteValueDictionary BuildParameterValuesFromExpression(MethodCallExpression methodCallExpression)
        {
            RouteValueDictionary result = new RouteValueDictionary();
            ParameterInfo[] parameters = methodCallExpression.Method.GetParameters();
            if (parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    object value;
                    var expressionArgument = methodCallExpression.Arguments[i];
                    if (expressionArgument.NodeType == ExpressionType.Constant)
                    {
                        // If argument is a constant expression, just get the value
                        value = (expressionArgument as ConstantExpression)?.Value ?? string.Empty /* ??? */;
                    }
                    else
                    {
                        try
                        {
                            // Otherwise, convert the argument subexpression to type object,
                            // make a lambda out of it, compile it, and invoke it to get the value
                            var convertExpression = Expression.Convert(expressionArgument, typeof(object));
                            value = Expression.Lambda<Func<object>>(convertExpression).Compile().Invoke();
                        }
                        catch
                        {
                            // ???
                            value = string.Empty;
                        }
                    }
                    result.Add(parameters[i].Name, value);
                }
            }
            return result;
        }
    }
}
