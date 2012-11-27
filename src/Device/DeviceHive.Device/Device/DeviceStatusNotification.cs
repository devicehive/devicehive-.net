using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a notification sent from <see cref="DeviceBase"/> descendants to update device status in the DeviceHive service.
    /// </summary>
    /// <remarks>
    /// These notifications allow DeviceHive to track device status so clients can retrieve it from the service.
    /// </remarks>
    public class DeviceStatusNotification : DeviceNotification
    {
        #region Public Properties

        /// <summary>
        /// Gets new device status.
        /// </summary>
        public string Status
        {
            get { return Parameters["status"]; }
            private set { Parameters["status"] = value; }
        }
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes device status.
        /// </summary>
        /// <param name="status">New device status.</param>
        public DeviceStatusNotification(string status)
            : base("deviceStatus")
        {
            if (string.IsNullOrEmpty(status))
                throw new ArgumentException("Status is null or empty!", "status");

            Status = status;
        }
        #endregion
    }
}
