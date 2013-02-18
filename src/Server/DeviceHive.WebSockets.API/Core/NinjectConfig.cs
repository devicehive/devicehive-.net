using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.MessageLogic.NotificationHandlers;
using DeviceHive.Core.Messaging;
using DeviceHive.Core.Services;
using DeviceHive.Data;
using DeviceHive.Data.EF;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using DeviceHive.WebSockets.API.Controllers;
using DeviceHive.WebSockets.API.Subscriptions;
using DeviceHive.WebSockets.Core.ActionsFramework;
using Ninject;

namespace DeviceHive.WebSockets.API.Core
{
    internal class NinjectConfig
    {
        public static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            RegisterServices(kernel);
            return kernel;
        }

        private static void RegisterServices(IKernel kernel)
        {
            // bind repositories
            kernel.Bind<IUserRepository, ISimpleRepository<User>>().To<UserRepository>();
            kernel.Bind<IUserNetworkRepository, ISimpleRepository<UserNetwork>>().To<UserNetworkRepository>();
            kernel.Bind<INetworkRepository, ISimpleRepository<Network>>().To<NetworkRepository>();
            kernel.Bind<IDeviceClassRepository, ISimpleRepository<DeviceClass>>().To<DeviceClassRepository>();
            kernel.Bind<IEquipmentRepository, ISimpleRepository<Equipment>>().To<EquipmentRepository>();
            kernel.Bind<IDeviceRepository, ISimpleRepository<Device>>().To<DeviceRepository>();
            kernel.Bind<IDeviceNotificationRepository, ISimpleRepository<DeviceNotification>>().To<DeviceNotificationRepository>();
            kernel.Bind<IDeviceCommandRepository, ISimpleRepository<DeviceCommand>>().To<DeviceCommandRepository>();
            kernel.Bind<IDeviceEquipmentRepository, ISimpleRepository<DeviceEquipment>>().To<DeviceEquipmentRepository>();

            // bind services
            kernel.Bind<DeviceService>().ToSelf();

            // bind controllers, router and action invoker
            kernel.Bind<ClientController>().ToSelf();
            kernel.Bind<DeviceController>().ToSelf();
            kernel.Bind<Router>().To<NinjectRouter>().InSingletonScope()
                .OnActivation(RoutesConfig.ConfigureRoutes);
            kernel.Bind<ActionInvoker>().ToSelf().InSingletonScope();

            // bind JSON mapper
            kernel.Bind<JsonMapperManager>().ToSelf().InSingletonScope()
                .OnActivation(JsonMapperConfig.ConfigureMapping);

            // bind data context
            kernel.Bind<DataContext>().ToSelf().InSingletonScope();

            // bind message bus
            kernel.Bind<MessageBus>().To<NamedPipeMessageBus>().InSingletonScope()
                .OnActivation(MessageBusConfig.ConfigureMessageBus);

            // bind subscription managers
            kernel.Bind<DeviceSubscriptionManager>().ToSelf().InSingletonScope().Named("DeviceCommand");
            kernel.Bind<DeviceSubscriptionManager>().ToSelf().InSingletonScope().Named("DeviceNotification");
            kernel.Bind<CommandSubscriptionManager>().ToSelf().InSingletonScope();

            // bind notification handlers
            kernel.Bind<IMessageManager>().To<MessageManager>().InSingletonScope();
            kernel.Bind<INotificationHandler>().To<DeviceStatusNotificationHandler>();
            kernel.Bind<INotificationHandler>().To<EquipmentNotificationHandler>();
        }
    }
}
