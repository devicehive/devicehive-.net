using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents an interface used as a channel from devices to DeviceHive.
    /// </summary>
    public interface IDeviceServiceChannel
    {
        /// <summary>
        /// Sends a notification to the DeviceHive service.
        /// </summary>
        /// <param name="notification"><see cref="DeviceNotification"/> object to send.</param>
        void SendNotification(DeviceNotification notification);
    }
}
