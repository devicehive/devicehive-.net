using System;
using DeviceHive.Data.Model;

namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Represents message handler context
    /// </summary>
    public class MessageHandlerContext
    {
        #region Public Properties

        /// <summary>
        /// Gets associated device
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// Gets associated notification
        /// </summary>
        public DeviceNotification Notification { get; private set; }

        /// <summary>
        /// Gets associated command
        /// </summary>
        public DeviceCommand Command { get; private set; }

        /// <summary>
        /// Gets the associated actor user
        /// </summary>
        public User User { get; private set; }

        /// <summary>
        /// Gets or sets flag indicating if message should be ignored by DeviceHive
        /// </summary>
        public bool IgnoreMessage { get; set; }

        #endregion

        #region Constrcutor

        /// <summary>
        /// Default constructor for notification messages
        /// </summary>
        /// <param name="notification">Associated DeviceNotification object</param>
        /// <param name="user">Associated User object</param>
        public MessageHandlerContext(DeviceNotification notification, User user = null)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");

            Device = notification.Device;
            Notification = notification;
            User = user;
        }

        /// <summary>
        /// Default constructor for command messages
        /// </summary>
        /// <param name="command">Associated DeviceCommand object</param>
        /// <param name="user">Associated User object</param>
        public MessageHandlerContext(DeviceCommand command, User user = null)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            Device = command.Device;
            Command = command;
            User = user;
        }
        #endregion
    }
}
