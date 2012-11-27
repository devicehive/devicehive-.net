using System;
using System.Collections.Generic;
using System.Threading;

namespace DeviceHive.Device
{
    /// <summary>
    /// Declares methods to connect a device with the DeviceHive service.
    /// Using this class, devices can register, send notifications and receive commands.
    /// </summary>
    public interface IDeviceService
    {
        /// <summary>
        /// Registers a device in the DeviceHive network.
        /// </summary>
        /// <param name="device"><see cref="Device"/> object.</param>
        /// <returns><see cref="Device"/> object registered.</returns>
        Device RegisterDevice(Device device);

        /// <summary>
        /// Sends new device notification to the service.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="notification">A <see cref="Notification"/> object</param>
        /// <returns>The <see cref="Notification"/> object with updated identifier and timestamp.</returns>
        Notification SendNotification(Guid deviceId, string deviceKey, Notification notification);

        /// <summary>
        /// Polls device commands from the service.
        /// This methods blocks the current thread until new command is received.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="timestamp">Last received command timestamp.</param>
        /// <param name="token">Cancellation token used to cancel polling operation.</param>
        /// <returns>A list of <see cref="Command"/> objects.</returns>
        List<Command> PollCommands(Guid deviceId, string deviceKey, DateTime timestamp, CancellationToken token);

        /// <summary>
        /// Updates a device command status and result.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="command">A <see cref="Command"/> object to be updated.</param>
        void UpdateCommand(Guid deviceId, string deviceKey, Command command);
    }
}
