using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents information about notification linked to a device.
    /// </summary>
    public class DeviceNotification
    {
        /// <summary>
        /// Associated subscription identifier.
        /// </summary>
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Device unique identifier.
        /// </summary>
        public string DeviceGuid { get; set; }

        /// <summary>
        /// <see cref="Notification"/> object.
        /// </summary>
        public Notification Notification { get; set; }
    }
}