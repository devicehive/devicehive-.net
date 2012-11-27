using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.API.Business
{
    public interface INotificationHandler
    {
        void Handle(DeviceNotification notification);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class HandleNotificationTypeAttribute : Attribute
    {
        #region Public Properties

        public string Type { get; private set; }

        #endregion

        #region Constructor

        public HandleNotificationTypeAttribute(string type)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentException("Type is null or empty!", "type");

            Type = type;
        }
        #endregion
    }
}
