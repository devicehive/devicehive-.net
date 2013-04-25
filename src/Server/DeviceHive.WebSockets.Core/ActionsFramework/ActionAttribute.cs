using System;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ActionAttribute : Attribute
    {
        public ActionAttribute(string actionName)
        {
            ActionName = actionName;
            NeedAuthentication = false;
        }

        public string ActionName { get; private set; }

        public bool NeedAuthentication { get; set; }
    }
}