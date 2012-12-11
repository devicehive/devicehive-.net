using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Provides information about <see cref="Notification"/> related event.
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        private readonly Notification _notification;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="notification">Notification object</param>
        public NotificationEventArgs(Notification notification)
        {
            _notification = notification;
        }

        /// <summary>
        /// Gets notification object
        /// </summary>
        public Notification Notification
        {
            get { return _notification; }
        }
    }

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