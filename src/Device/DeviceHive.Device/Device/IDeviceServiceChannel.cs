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

        /// <summary>
        /// Sends a notification to the DeviceHive service.
        /// </summary>
        /// <param name="notification">Notification name.</param>
        /// <param name="parameters">A custom strongly-typed object with notification parameters.</param>
        void SendNotification(string notification, object parameters);

        /// <summary>
        /// Sends an equipment notification to the DeviceHive service.
        /// These notifications allow DeviceHive to track the latest state of the equipment so clients can retrieve it from the service.
        /// </summary>
        /// <param name="equipment">Equipment code as set in <see cref="DeviceEquipmentInfo.Code"/> property.</param>
        /// <param name="parameters">A custom strongly-typed object with equipment notification parameters.</param>
        void SendEquipmentNotification(string equipment, object parameters);
    }
}
