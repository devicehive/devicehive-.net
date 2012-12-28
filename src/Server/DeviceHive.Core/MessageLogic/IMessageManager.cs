using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Represents interface for notification managers that can process inbound messages
    /// </summary>
    public interface IMessageManager
    {
        /// <summary>
        /// Processes income notificaton
        /// </summary>
        /// <param name="notification">DeviceNotification object</param>
        void ProcessNotification(DeviceNotification notification);
    }
}
