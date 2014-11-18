using System;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PingAttribute : Attribute
    {
        public PingAttribute()
        {
        }
    }
}