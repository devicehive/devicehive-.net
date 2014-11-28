using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// Provides information about <see cref="Command"/> related event.
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        #region Public Properties

        /// <summary>
        /// Gets device unique identifier.
        /// </summary>
        public string DeviceGuid { get; private set; }

        /// <summary>
        /// Gets command object.
        /// </summary>
        public Command Command { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">Command object.</param>
        public CommandEventArgs(string deviceGuid, Command command)
        {
            DeviceGuid = deviceGuid;
            Command = command;
        }
        #endregion
    }
}