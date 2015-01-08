using System;
using System.Collections.Generic;
using DeviceHive.API.Internal;
using DeviceHive.Core.Messaging;
using Ninject;
using Ninject.Activation;

namespace DeviceHive.API
{
    public class MessageBusConfig
    {
        public static void ConfigureSubscriptions(IContext context, MessageBus messageBus)
        {
            var notificationByDeviceIdWaiter = context.Kernel.Get<ObjectWaiter>("DeviceNotification.DeviceID");
            var commandByDeviceIdWaiter = context.Kernel.Get<ObjectWaiter>("DeviceCommand.DeviceID");
            var commandByCommandIdWaiter = context.Kernel.Get<ObjectWaiter>("DeviceCommand.CommandID");

            messageBus.Subscribe<DeviceNotificationAddedMessage>(message => notificationByDeviceIdWaiter.NotifyChanges(message.DeviceId));
            messageBus.Subscribe<DeviceCommandAddedMessage>(message => commandByDeviceIdWaiter.NotifyChanges(message.DeviceId));
            messageBus.Subscribe<DeviceCommandUpdatedMessage>(message => commandByCommandIdWaiter.NotifyChanges(message.CommandId));
        }
    }
}