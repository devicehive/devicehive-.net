using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;
using log4net;

namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Represents default implementation of the IMessageManager interface
    /// </summary>
    public class MessageManager : IMessageManager
    {
        private readonly ILookup<string, INotificationHandler> _notificationHandlers;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="handlers">Array of notification handlers</param>
        public MessageManager(INotificationHandler[] notificationHandlers)
        {
            _notificationHandlers = notificationHandlers
                .SelectMany(h => h.NotificationTypes.Select(nt => new { Handler = h, Type = nt }))
                .ToLookup(h => h.Type, h => h.Handler, StringComparer.OrdinalIgnoreCase);
        }
        #endregion

        #region IMessageManager Members

        /// <summary>
        /// Processes income notificaton
        /// </summary>
        /// <param name="notification">DeviceNotification object</param>
        public void ProcessNotification(DeviceNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");

            foreach (var handler in _notificationHandlers[notification.Notification])
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