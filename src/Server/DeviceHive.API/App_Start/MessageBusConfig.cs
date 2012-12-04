using System;
using System.Collections.Generic;
using DeviceHive.API.Business;
using DeviceHive.Core.Messaging;
using Ninject;
using Ninject.Activation;

namespace DeviceHive.API
{
    public class MessageBusConfig
    {
        public static void ConfigureSubscriptions(IContext context, MessageBus messageBus)
        {
            var notificationWaiter = context.Kernel.Get<ObjectWaiter>("DeviceNotification");
            var commandWaiter = context.Kernel.Get<ObjectWaiter>("DeviceCommand");
            
            messageBus.Subscribe<DeviceNotificationAddedMessage>(message => notificationWaiter.NotifyChanges(message.DeviceId));
            messageBus.Subscribe<DeviceCommandAddedMessage>(message => commandWaiter.NotifyChanges(message.DeviceId));
        }
    }
}