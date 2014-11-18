using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;
using Ninject;

namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Represents interface for notification managers that can process inbound messages.
    /// </summary>
    public interface IMessageManager
    {
        /// <summary>
        /// Initializes message manager.
        /// Reads the configiration and instantiates enabled message handlers.
        /// </summary>
        /// <param name="kernel">NInject kernel.</param>
        void Initialize(IKernel kernel);
        
        /// <summary>
        /// Handles the incoming notification.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        void HandleNotification(MessageHandlerContext context);

        /// <summary>
        /// Handles the incoming command.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        void HandleCommand(MessageHandlerContext context);

        /// <summary>
        /// Handles the command update request.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        void HandleCommandUpdate(MessageHandlerContext context);
    }
}
