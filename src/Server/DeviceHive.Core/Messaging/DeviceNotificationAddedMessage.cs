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
        /// Gets or sets device unique identifier
        /// </summary>
        public Guid DeviceGuid { get; set; }

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
        /// <param name="deviceGuid">Device unique identifier</param>
        /// <param name="notificationId">Notification identifier</param>
        public DeviceNotificationAddedMessage(Guid deviceGuid, int notificationId)
        {
            DeviceGuid = deviceGuid;
            NotificationId = notificationId;
        }
        #endregion
    }
}
