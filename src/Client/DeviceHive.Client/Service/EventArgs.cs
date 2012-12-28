using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Provides information about <see cref="Notification"/> related event.
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        private readonly Guid _deviceGuid;       
        private readonly Notification _notification;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="deviceGuid">Device GUID</param>
        /// <param name="notification">Notification object</param>        
        public NotificationEventArgs(Guid deviceGuid, Notification notification)
        {
            _deviceGuid = deviceGuid;
            _notification = notification;            
        }

        /// <summary>
        /// Gets device GUID
        /// </summary>
        public Guid DeviceGuid
        {
            get { return _deviceGuid; }
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