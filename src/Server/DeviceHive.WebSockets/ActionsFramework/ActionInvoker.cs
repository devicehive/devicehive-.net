using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DeviceHive.WebSockets.ActionsFramework
{
    public sealed class ActionInvoker
    {
        private readonly ControllerCollection _controllerCollection = new ControllerCollection();

        public void InvokeAction(ControllerBase controller, string actionName)
        {
            var actionCollection = _controllerCollection.GetActionCollection(controller.GetType());
            var action = actionCollection.GetAction(actionName);

            if (action == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Can't find action {0} in controller {1}",
                    actionName, controller.GetType().FullName));
            }

            action.Invoke(controller);
        }


        #region Inner classes

        private class ActionInfo
        {
            private readonly bool _needAuthentication;
            private readonly Action<ControllerBase> _invokeAction;

            public ActionInfo(MethodInfo methodInfo, bool needAuthentication)
            {
                var controllerType = methodInfo.DeclaringType;
                var controllerParam = Expression.Parameter(typeof(ControllerBase));

                var typedControllerExpr = Expression.Convert(controllerParam, controllerType);
                var actionCallExpr = Expression.Call(typedControllerExpr, methodInfo);
                var lambda = Expression.Lambda<Action<ControllerBase>>(actionCallExpr, controllerParam);

                _invokeAction = lambda.Compile();
                _needAuthentication = needAuthentication;
            }

            public void Invoke(ControllerBase controller)
            {
                if (_needAuthentication && !controller.IsAuthenticated)
                {
                    return;
                }

                _invokeAction(controller);
            }
        }

        private class ActionCollection
        {
            private readonly Dictionary<string, ActionInfo> _actions =
                new Dictionary<string, ActionInfo>();

            public ActionCollection(Type controllerType)
            {
                FindActions(controllerType);
            }

            public ActionInfo GetAction(string name)
            {
                ActionInfo action;
                return _actions.TryGetValue(name, out action) ? action : null;
            }

            private void FindActions(Type controllerType)
            {
                var methods = controllerType.GetMethods();

                foreach (var methodInfo in methods)
                {
                    var actionAttrs = methodInfo.GetCustomAttributes(typeof (ActionAttribute), true);
                    if (actionAttrs.Length == 0)
                        continue;

                    var actionAttr = (ActionAttribute) actionAttrs[0];
                    var action = new ActionInfo(methodInfo, actionAttr.NeedAuthentication);
                    _actions.Add(actionAttr.ActionName, action);
                }
            }
        }

        private class ControllerCollection
        {
            private readonly object _lock = new object();

            private readonly Dictionary<Type, ActionCollection> _actionCollections =
                new Dictionary<Type, ActionCollection>();

            public ActionCollection GetActionCollection(Type controllerType)
            {
                ActionCollection actionCollection;

                if (_actionCollections.TryGetValue(controllerType, out actionCollection))
                    return actionCollection;

                lock (_lock)
                {
                    if (_actionCollections.TryGetValue(controllerType, out actionCollection))
                        return actionCollection;

                    actionCollection = new ActionCollection(controllerType);
                    _actionCollections.Add(controllerType, actionCollection);
                    return actionCollection;
                }
            }
        }

        #endregion
    }
}