using DeviceHive.Core.Messaging;
using DeviceHive.WebSockets.API.Controllers;
using Ninject;
using Ninject.Activation;

namespace DeviceHive.WebSockets.API.Core
{
    public static class MessageBusConfig
    {
        public static void ConfigureMessageBus(IContext context, MessageBus messageBus)
        {
            var clientController = context.Kernel.Get<ClientController>();
            var deviceController = context.Kernel.Get<DeviceController>();

            messageBus.Subscribe((DeviceNotificationAddedMessage msg) =>
                clientController.HandleDeviceNotification(msg.DeviceId, msg.NotificationId));

            messageBus.Subscribe((DeviceCommandAddedMessage msg) =>
                {
                    deviceController.HandleDeviceCommand(msg.DeviceId, msg.CommandId);
                    clientController.HandleDeviceCommand(msg.DeviceId, msg.CommandId);
                });

            messageBus.Subscribe((DeviceCommandUpdatedMessage msg) =>
                clientController.HandleCommandUpdate(msg.CommandId));
        }
    }
}