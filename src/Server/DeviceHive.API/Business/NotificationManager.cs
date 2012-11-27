using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.API.Business.NotificationHandlers;
using DeviceHive.Data.Model;
using log4net;

namespace DeviceHive.API.Business
{
    public interface INotificationManager
    {
        void ProcessNotification(DeviceNotification notification);
    }

    public class NotificationManager : INotificationManager
    {
        private ILookup<string, INotificationHandler> _handlers;

        #region Constructor

        public NotificationManager(INotificationHandler[] handlers)
        {
            _handlers = handlers.Select(h => new
                {
                    Handler = h,
                    Type = (HandleNotificationTypeAttribute)h.GetType().GetCustomAttributes(typeof(HandleNotificationTypeAttribute), false).FirstOrDefault(),
                })
                .Where(h => h.Type != null)
                .ToLookup(h => h.Type.Type, h => h.Handler, StringComparer.OrdinalIgnoreCase);
        }
        #endregion

        #region INotificationManager Members

        public void ProcessNotification(DeviceNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");

            foreach (var handler in _handlers[notification.Notification])
            {
                try
                {
                    handler.Handle(notification);
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger(GetType()).Error("Notification handler generated exception!", ex);
                }
            }
        }
        #endregion
    }
}