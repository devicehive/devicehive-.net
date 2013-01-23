using System;
using System.Reflection;
using System.Web;
using DeviceHive.API.Business;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.MessageLogic.NotificationHandlers;
using DeviceHive.Core.Messaging;
using DeviceHive.Core.Services;
using DeviceHive.Data;
using DeviceHive.Data.EF;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Ninject;
using Ninject.Web.Common;

[assembly: WebActivator.PreApplicationStartMethod(typeof(DeviceHive.API.NinjectWebCommon), "Start")]
[assembly: WebActivator.ApplicationShutdownMethodAttribute(typeof(DeviceHive.API.NinjectWebCommon), "Stop")]

namespace DeviceHive.API
{
    public static class NinjectWebCommon
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

            RegisterServices(kernel);
            return kernel;
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            // load assembly modules
            kernel.Load(Assembly.GetExecutingAssembly());

            // bind repositories
            kernel.Bind<ITimestampRepository>().To<TimestampRepository>();
            kernel.Bind<IUserRepository, ISimpleRepository<User>>().To<UserRepository>();
            kernel.Bind<IUserNetworkRepository, ISimpleRepository<UserNetwork>>().To<UserNetworkRepository>();
            kernel.Bind<INetworkRepository, ISimpleRepository<Network>>().To<NetworkRepository>();
            kernel.Bind<IDeviceClassRepository, ISimpleRepository<DeviceClass>>().To<DeviceClassRepository>();
            kernel.Bind<IEquipmentRepository, ISimpleRepository<Equipment>>().To<EquipmentRepository>();
            kernel.Bind<IDeviceRepository, ISimpleRepository<Device>>().To<DeviceRepository>();
            kernel.Bind<IDeviceNotificationRepository, ISimpleRepository<DeviceNotification>>().To<DeviceNotificationRepository>();
            kernel.Bind<IDeviceCommandRepository, ISimpleRepository<DeviceCommand>>().To<DeviceCommandRepository>();
            kernel.Bind<IDeviceEquipmentRepository, ISimpleRepository<DeviceEquipment>>().To<DeviceEquipmentRepository>();

            // bind data context
            kernel.Bind<DataContext>().ToSelf().InSingletonScope();

            // bind services
            kernel.Bind<DeviceService>().ToSelf();

            // bind json mapper
            kernel.Bind<JsonMapperManager>().ToSelf().InSingletonScope().OnActivation(JsonMapperConfig.ConfigureMapping);

            // bind message bus
            kernel.Bind<MessageBus>().To<NamedPipeMessageBus>().InSingletonScope().OnActivation(MessageBusConfig.ConfigureSubscriptions);

            // bind object waiters
            kernel.Bind<ObjectWaiter>().ToSelf().InSingletonScope().Named("DeviceNotification.DeviceID");
            kernel.Bind<ObjectWaiter>().ToSelf().InSingletonScope().Named("DeviceCommand.DeviceID");
            kernel.Bind<ObjectWaiter>().ToSelf().InSingletonScope().Named("DeviceCommand.CommandID");

            // bind message logic handlers
            kernel.Bind<IMessageManager>().To<MessageManager>().InSingletonScope();
            kernel.Bind<INotificationHandler>().To<DeviceStatusNotificationHandler>();
            kernel.Bind<INotificationHandler>().To<EquipmentNotificationHandler>();

            // bind request context
            kernel.Bind<RequestContext>().ToSelf().InRequestScope();
        }
    }
}
