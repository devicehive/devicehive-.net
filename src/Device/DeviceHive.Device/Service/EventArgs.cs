using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// Provides information about <see cref="Command"/> related event.
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        private readonly Guid _deviceGuid;
        private readonly Command _command;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">Command object.</param>
        public CommandEventArgs(Guid deviceGuid, Command command)
        {
            _command = command;
            _deviceGuid = deviceGuid;
        }

        /// <summary>
        /// Gets device unique identifier
        /// </summary>
        public Guid DeviceGuid
        {
            get { return _deviceGuid; }
        }

        /// <summary>
        /// Gets command object
        /// </summary>
        public Command Command
        {
            get { return _command; }
        }
    }
}