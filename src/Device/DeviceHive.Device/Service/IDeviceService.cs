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
        /// Gets device from the DeviceHive network.
        /// </summary>
        /// <param name="device"><see cref="Device"/> object with a valid unique identifier and key.</param>
        /// <returns><see cref="Device"/> object from DeviceHive.</returns>
        Device GetDevice(Device device);

        /// <summary>
        /// Registers a device in the DeviceHive network.
        /// </summary>
        /// <param name="device"><see cref="Device"/> object.</param>
        void RegisterDevice(Device device);

        /// <summary>
        /// Updates a device in the DeviceHive network.
        /// </summary>
        /// <param name="device"><see cref="Device"/> object.</param>
        void UpdateDevice(Device device);

        /// <summary>
        /// Sends new device notification to the service.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="notification">A <see cref="Notification"/> object</param>
        /// <returns>The <see cref="Notification"/> object with updated identifier and timestamp.</returns>
        Notification SendNotification(string deviceId, string deviceKey, Notification notification);

        /// <summary>
        /// Polls device commands from the service.
        /// This methods blocks the current thread until new command is received.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="timestamp">Last received command timestamp.</param>
        /// <param name="token">Cancellation token used to cancel polling operation.</param>
        /// <returns>A list of <see cref="Command"/> objects.</returns>
        List<Command> PollCommands(string deviceId, string deviceKey, DateTime? timestamp, CancellationToken token);

        /// <summary>
        /// Subscribe to device commands
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <remarks>
        /// Subscription can be removed through <see cref="UnsubscribeFromCommands"/> method
        /// </remarks>
        void SubscribeToCommands(string deviceId, string deviceKey);

        /// <summary>
        /// Unsubscribe from device notifications
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        void UnsubscribeFromCommands(string deviceId, string deviceKey);

        /// <summary>
        /// Updates a device command status and result.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="command">A <see cref="Command"/> object to be updated.</param>
        void UpdateCommand(string deviceId, string deviceKey, Command command);

        /// <summary>
        /// Fires when new command inserted for some active command subscription.
        /// </summary>
        /// <remarks>
        /// Subscription can be created through <see cref="IDeviceService.SubscribeToCommands"/> method.
        /// </remarks>
        event EventHandler<CommandEventArgs> CommandInserted;

        /// <summary>
        /// Fires when underlying connection is closed
        /// </summary>
        event EventHandler ConnectionClosed;
    }
}
