using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DeviceHive.WebSockets.ActionsFramework
{
    internal class ActionInfo
    {
        private readonly bool _needAuthentication;
        private readonly Action<ControllerBase> _invokeAction;

        public ActionInfo(MethodInfo methodInfo, bool needAuthentication)
        {
            _invokeAction = BuildInvokeAction(methodInfo);
            _needAuthentication = needAuthentication;
        }

        public void Invoke(ControllerBase controller)
        {
            if (_needAuthentication && !controller.IsAuthenticated)
                throw new WebSocketRequestException("Please authenticate to invoke this action");

            _invokeAction(controller);
        }

        private Action<ControllerBase> BuildInvokeAction(MethodInfo methodInfo)
        {
            var controllerType = methodInfo.DeclaringType;
            var controllerParam = Expression.Parameter(typeof(ControllerBase));

            var typedControllerExpr = Expression.Convert(controllerParam, controllerType);
            var parameters = methodInfo
                .GetParameters()
                .Select(parameterInfo => BuildParameterExpression(controllerParam, parameterInfo))
                .ToArray();

            var actionCallExpr = Expression.Call(typedControllerExpr, methodInfo, parameters);
            var lambda = Expression.Lambda<Action<ControllerBase>>(actionCallExpr, controllerParam);

            return lambda.Compile();
        }

        private Expression BuildParameterExpression(ParameterExpression controllerParam,
            ParameterInfo parameterInfo)
        {
            var parameterType = parameterInfo.ParameterType;
            
            var controllerType = typeof (ControllerBase);
            var getArgumentMethod = controllerType
                .GetMethod("GetArgument")
                .MakeGenericMethod(parameterType);

            return Expression.Call(controllerParam, getArgumentMethod,
                Expression.Constant(parameterInfo.Name));
        }
    }
}