using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Provides information about <see cref="Notification"/> related event.
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        #region Public Properties

        /// <summary>
        /// Gets device GUID
        /// </summary>
        public Guid DeviceGuid { get; private set; }

        /// <summary>
        /// Gets notification object
        /// </summary>
        public Notification Notification { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="deviceGuid">Device GUID</param>
        /// <param name="notification">Notification object</param>
        public NotificationEventArgs(Guid deviceGuid, Notification notification)
        {
            DeviceGuid = deviceGuid;
            Notification = notification;
        }
        #endregion
    }
}