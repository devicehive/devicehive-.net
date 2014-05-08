using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents information about command linked to a device.
    /// </summary>
    public class DeviceCommand
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
        /// <see cref="Command"/> object.
        /// </summary>
        public Command Command { get; set; }
    }
}