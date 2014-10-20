using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Represents base class for custom message handlers.
    /// </summary>
    public abstract class MessageHandler
    {
        /// <summary>
        /// Invoked when a new notification is retrieved, but not yet processed.
        /// Set context.IgnoreMessage to True to ignore the notification.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public virtual void NotificationReceived(MessageHandlerContext context)
        {
        }

        /// <summary>
        /// Invoked when a new notification is inserted.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public virtual void NotificationInserted(MessageHandlerContext context)
        {
        }

        /// <summary>
        /// Invoked when a new command is retrieved, but not yet processed.
        /// Set context.IgnoreMessage to True to ignore the command.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public virtual void CommandReceived(MessageHandlerContext context)
        {
        }

        /// <summary>
        /// Invoked when a new command is inserted.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public virtual void CommandInserted(MessageHandlerContext context)
        {
        }

        /// <summary>
        /// Invoked when an existing command is updated.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public virtual void CommandUpdated(MessageHandlerContext context)
        {
        }
    }
}
