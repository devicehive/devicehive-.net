using System;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// A message about new device notification being added
    /// </summary>
    public class DeviceNotificationAddedMessage
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets device identifier
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets notification identifier
        /// </summary>
        public int NotificationId { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeviceNotificationAddedMessage()
        {
        }

        /// <summary>
        /// Specifies device and notification identifiers
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <param name="notificationId">Notification identifier</param>
        public DeviceNotificationAddedMessage(int deviceId, int notificationId)
        {
            DeviceId = deviceId;
            NotificationId = notificationId;
        }
        #endregion
    }
}
