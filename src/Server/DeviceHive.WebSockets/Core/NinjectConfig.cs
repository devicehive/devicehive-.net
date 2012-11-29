using System;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.EF;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using DeviceHive.WebSockets.Controllers;
using DeviceHive.WebSockets.Network;
using DeviceHive.WebSockets.Subscriptions;
using Ninject;

namespace DeviceHive.WebSockets.Core
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
			kernel.Bind<INetworkRepository, ISimpleRepository<Data.Model.Network>>().To<NetworkRepository>();
			kernel.Bind<IDeviceClassRepository, ISimpleRepository<DeviceClass>>().To<DeviceClassRepository>();
			kernel.Bind<IEquipmentRepository, ISimpleRepository<Equipment>>().To<EquipmentRepository>();
			kernel.Bind<IDeviceRepository, ISimpleRepository<Device>>().To<DeviceRepository>();
			kernel.Bind<IDeviceNotificationRepository, ISimpleRepository<DeviceNotification>>().To<DeviceNotificationRepository>();
			kernel.Bind<IDeviceCommandRepository, ISimpleRepository<DeviceCommand>>().To<DeviceCommandRepository>();
			kernel.Bind<IDeviceEquipmentRepository, ISimpleRepository<DeviceEquipment>>().To<DeviceEquipmentRepository>();

			// bind controllers and router
			kernel.Bind<ClientController>().ToSelf();
			kernel.Bind<DeviceController>().ToSelf();
			kernel.Bind<Router>().ToSelf();

			// bind JSON mapper
			kernel.Bind<JsonMapperManager>().ToSelf().InSingletonScope().OnActivation(JsonMapperConfig.ConfigureMapping);

			// bind data context
			kernel.Bind<DataContext>().ToSelf().InSingletonScope();

			// bind message bus
			kernel.Bind<MessageBus>().To<NamedPipeMessageBus>().InSingletonScope();

			// bind subscription managers
			kernel.Bind<SubscriptionManager>().ToSelf().InSingletonScope().Named("DeviceCommand");
			kernel.Bind<SubscriptionManager>().ToSelf().InSingletonScope().Named("DeviceNotification");

			// bind web socket server
			kernel.Bind<WebSocketServerBase>().To<FleckWebSocketServer>().InSingletonScope();
			kernel.Bind<WebSocketService>().ToSelf().InSingletonScope();
		}
	}
}
