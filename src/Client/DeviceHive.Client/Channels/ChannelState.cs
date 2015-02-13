using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents enumeration of available channel states.
    /// </summary>
    public enum ChannelState
    {
        /// <summary>
        /// The channel is not connected to the DeviceHive server.
        /// </summary>
        Disconnected,

        /// <summary>
        /// The channel is currently connecting to the DeviceHive server.
        /// </summary>
        Connecting,

        /// <summary>
        /// The channel is connected to the DeviceHive server.
        /// </summary>
        Connected,

        /// <summary>
        /// The channel is currently reconnecting to the DeviceHive server.
        /// </summary>
        Reconnecting,
    }
}
