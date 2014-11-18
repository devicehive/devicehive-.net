using System;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ActionAttribute : Attribute
    {
        public ActionAttribute(string actionName)
        {
            ActionName = actionName;
        }

        public string ActionName { get; private set; }
    }
}