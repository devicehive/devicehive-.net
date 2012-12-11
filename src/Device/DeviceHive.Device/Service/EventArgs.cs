using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// Provides information about <see cref="Command"/> related event.
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        private readonly Command _command;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="command">Command object</param>
        public CommandEventArgs(Command command)
        {
            _command = command;
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