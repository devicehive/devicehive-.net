using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Represents interface for special notification handlers
    /// </summary>
    public interface INotificationHandler
    {
        /// <summary>
        /// Gets array of supported notification types
        /// </summary>
        string[] NotificationTypes { get; }

        /// <summary>
        /// Handles a device notification
        /// </summary>
        /// <param name="notification">DeviceNotification object</param>
        void Handle(DeviceNotification notification);
    }
}
