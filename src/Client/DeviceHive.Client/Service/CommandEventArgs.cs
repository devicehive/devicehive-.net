using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Provides information about <see cref="Command"/> related event.
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        #region Public Properties

        /// <summary>
        /// Gets command object
        /// </summary>
        public Command Command { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="command">Command object</param>
        public CommandEventArgs(Command command)
        {
            Command = command;
        }
        #endregion
    }
}
