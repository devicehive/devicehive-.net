using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    internal class ActionInfo
    {
        private readonly Action<ActionContext> _invokeAction;
        private readonly ActionFilterAttribute[] _filters;

        public ActionInfo(MethodInfo methodInfo)
        {
            _invokeAction = BuildInvokeAction(methodInfo);
            _filters = methodInfo.GetCustomAttributes(typeof(ActionFilterAttribute), true).Cast<ActionFilterAttribute>().ToArray();
        }

        public void Invoke(ActionContext actionContext)
        {
            foreach (var filter in _filters)
                filter.OnAuthentication(actionContext);

            foreach (var filter in _filters)
                filter.OnAuthorization(actionContext);

            _invokeAction(actionContext);
        }

        private Action<ActionContext> BuildInvokeAction(MethodInfo methodInfo)
        {
            var controllerType = methodInfo.DeclaringType;
            var actionContextParam = Expression.Parameter(typeof(ActionContext));

            var controllerExpr = Expression.Property(actionContextParam, typeof(ActionContext).GetProperty("Controller"));
            var typedControllerExpr = Expression.Convert(controllerExpr, controllerType);
            var parameters = methodInfo.GetParameters()
                .Select(parameterInfo => BuildParameterExpression(actionContextParam, parameterInfo)).ToArray();

            var actionCallExpr = Expression.Call(typedControllerExpr, methodInfo, parameters);
            var lambda = Expression.Lambda<Action<ActionContext>>(actionCallExpr, actionContextParam);

            return lambda.Compile();
        }

        private Expression BuildParameterExpression(ParameterExpression actionContextParam,
            ParameterInfo parameterInfo)
        {
            var parameterType = parameterInfo.ParameterType;

            var getParameterMethod = typeof(ActionContext)
                .GetMethod("GetRequestParameter")
                .MakeGenericMethod(parameterType);

            return Expression.Call(actionContextParam, getParameterMethod,
                Expression.Constant(parameterInfo.Name));
        }
    }
}