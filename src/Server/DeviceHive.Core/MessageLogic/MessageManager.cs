using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.Repositories;
using log4net;
using Ninject;
using Ninject.Parameters;

namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Represents default implementation of the IMessageManager interface.
    /// </summary>
    public class MessageManager : IMessageManager
    {
        private readonly MessageBus _messageBus;
        private readonly DeviceHiveConfiguration _configuration;
        private readonly IDeviceNotificationRepository _deviceNotificationRepository;
        private readonly IDeviceCommandRepository _deviceCommandRepository;
        private readonly List<MessageHandlerInfo> _messageHandlerInfos;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="messageBus">MessageBus object</param>
        /// <param name="configuration">DeviceHive configuration</param>
        /// <param name="deviceNotificationRepository">IDeviceNotificationRepository object</param>
        /// <param name="deviceCommandRepository">IDeviceCommandRepository object</param>
        public MessageManager(MessageBus messageBus, DeviceHiveConfiguration configuration,
            IDeviceNotificationRepository deviceNotificationRepository, IDeviceCommandRepository deviceCommandRepository)
        {
            _messageBus = messageBus;
            _configuration = configuration;
            _deviceNotificationRepository = deviceNotificationRepository;
            _deviceCommandRepository = deviceCommandRepository;
            _messageHandlerInfos = new List<MessageHandlerInfo>();
        }
        #endregion

        #region IMessageManager Members

        /// <summary>
        /// Initializes message manager.
        /// Reads the configiration and instantiates enabled message handlers.
        /// </summary>
        /// <param name="kernel">NInject kernel.</param>
        public void Initialize(IKernel kernel)
        {
            var messageHandlers = _configuration.MessageHandlers.Cast<MessageHandlerConfigurationElement>().ToArray();
            foreach (var messageHandler in messageHandlers)
            {
                var messageHandlerType = Type.GetType(messageHandler.Type, false);
                if (messageHandlerType == null)
                {
                    throw new Exception(string.Format("Could not load type: '{0}'!" +
                        " Please put the all referenced assemblies into the DeviceHive executable folder.", messageHandler.Type));
                }
                if (!typeof(MessageHandler).IsAssignableFrom(messageHandlerType))
                {
                    throw new Exception(string.Format("The type '{0}' must implement MessageHandler" +
                        " in order to be registered as message handler!", messageHandlerType));
                }

                var notificationNames = string.IsNullOrEmpty(messageHandler.NotificationNames) ? null : messageHandler.NotificationNames.Split(',').Select(c => c.Trim()).ToArray();
                var commandNames = string.IsNullOrEmpty(messageHandler.CommandNames) ? null : messageHandler.CommandNames.Split(',').Select(c => c.Trim()).ToArray();
                var deviceGuids = string.IsNullOrEmpty(messageHandler.DeviceGuids) ? null : messageHandler.DeviceGuids.Split(',').Select(c => c.Trim()).ToArray();

                var deviceClassIdsString = string.IsNullOrEmpty(messageHandler.DeviceClassIds) ? null : messageHandler.DeviceClassIds.Split(',').Select(c => c.Trim()).ToArray();
                if (deviceClassIdsString != null && deviceClassIdsString.Any(n => !Regex.IsMatch(n, @"^\d+$")))
                    throw new Exception(string.Format("Invalid format of deviceClassIds setting: '{0}'", messageHandler.DeviceClassIds));
                var deviceClassIds = deviceClassIdsString == null ? null : deviceClassIdsString.Select(n => int.Parse(n)).ToArray();

                var networkIdsString = string.IsNullOrEmpty(messageHandler.NetworkIds) ? null : messageHandler.NetworkIds.Split(',').Select(c => c.Trim()).ToArray();
                if (networkIdsString != null && networkIdsString.Any(n => !Regex.IsMatch(n, @"^\d+$")))
                    throw new Exception(string.Format("Invalid format of networkIds setting: '{0}'", messageHandler.NetworkIds));
                var networkIds = networkIdsString == null ? null : networkIdsString.Select(n => int.Parse(n)).ToArray();

                var messageHandlerInstance = (MessageHandler)kernel.Get(messageHandlerType, new ConstructorArgument("argument", messageHandler.Argument));
                _messageHandlerInfos.Add(new MessageHandlerInfo(messageHandlerInstance, notificationNames, commandNames, deviceGuids, deviceClassIds, networkIds));
            }
        }

        /// <summary>
        /// Handles the incoming notification.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public void HandleNotification(MessageHandlerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Notification == null)
                throw new ArgumentNullException("context.Notification");

            // prepare array of matching handlers
            var handlers = _messageHandlerInfos.Where(info => !IsFiltered(info, context)).Select(i => i.MessageHandler).ToArray();

            // invoke NotificationReceived on handlers; return if IgnoreMessage was set
            InvokeHandlers(handlers, context, handler => handler.NotificationReceived(context));
            if (context.IgnoreMessage)
                return;

            // save notification into the database
            _deviceNotificationRepository.Save(context.Notification);

            // invoke NotificationInserted on handlers
            InvokeHandlers(handlers, context, handler => handler.NotificationInserted(context));

            // notify other nodes about new notification
            _messageBus.Notify(new DeviceNotificationAddedMessage(context.Device.ID, context.Notification.ID));
        }

        /// <summary>
        /// Handles the incoming command.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public void HandleCommand(MessageHandlerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Command == null)
                throw new ArgumentNullException("context.Command");

            // prepare array of matching handlers
            var handlers = _messageHandlerInfos.Where(info => !IsFiltered(info, context)).Select(i => i.MessageHandler).ToArray();

            // invoke CommandReceived on handlers; return if IgnoreMessage was set
            InvokeHandlers(handlers, context, handler => handler.CommandReceived(context));
            if (context.IgnoreMessage)
                return;

            // save command into the database
            _deviceCommandRepository.Save(context.Command);

            // invoke CommandInserted on handlers
            InvokeHandlers(handlers, context, handler => handler.CommandInserted(context));

            // notify other nodes about new command
            _messageBus.Notify(new DeviceCommandAddedMessage(context.Device.ID, context.Command.ID));
        }

        /// <summary>
        /// Handles the command update request.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public void HandleCommandUpdate(MessageHandlerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Command == null)
                throw new ArgumentNullException("context.Command");

            // prepare array of matching handlers
            var handlers = _messageHandlerInfos.Where(info => !IsFiltered(info, context)).Select(i => i.MessageHandler).ToArray();

            // save command into the database
            _deviceCommandRepository.Save(context.Command);

            // invoke CommandUpdated on handlers
            InvokeHandlers(handlers, context, handler => handler.CommandUpdated(context));

            // notify other nodes about command update
            _messageBus.Notify(new DeviceCommandUpdatedMessage(context.Device.ID, context.Command.ID));
        }
        #endregion

        #region Private Methods

        private bool IsFiltered(MessageHandlerInfo info, MessageHandlerContext context)
        {
            if (context.Notification != null && info.NotificationNames != null && !info.NotificationNames.Contains(context.Notification.Notification))
                return true;
            if (context.Command != null && info.CommandNames != null && !info.CommandNames.Contains(context.Command.Command))
                return true;
            if (info.DeviсeGuids != null && !info.DeviсeGuids.Contains(context.Device.GUID, StringComparer.OrdinalIgnoreCase))
                return true;
            if (info.DeviсeClassIds != null && !info.DeviсeClassIds.Contains(context.Device.DeviceClassID))
                return true;
            if (info.NetworkIds != null && !info.NetworkIds.Contains(context.Device.NetworkID ?? 0))
                return true;
            
            return false;
        }

        private void InvokeHandlers(MessageHandler[] handlers, MessageHandlerContext context, Action<MessageHandler> action)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    action(handler);
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger(GetType()).Error("Message handler generated exception!", ex);
                }

                if (context.IgnoreMessage)
                    break;
            }
        }
        #endregion
    }
}