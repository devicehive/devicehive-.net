using System;
using System.Reflection;
using System.Web;
using Ninject;
using Ninject.Web.Common;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using DeviceHive.API.Business;
using DeviceHive.API.Business.NotificationHandlers;
using DeviceHive.API.Mapping;
using DeviceHive.Data.EF;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

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
            kernel.Bind<IUserRepository, ISimpleRepository<User>>().To<UserRepository>();
            kernel.Bind<IUserNetworkRepository, ISimpleRepository<UserNetwork>>().To<UserNetworkRepository>();
            kernel.Bind<INetworkRepository, ISimpleRepository<Network>>().To<NetworkRepository>();
            kernel.Bind<IDeviceClassRepository, ISimpleRepository<DeviceClass>>().To<DeviceClassRepository>();
            kernel.Bind<IEquipmentRepository, ISimpleRepository<Equipment>>().To<EquipmentRepository>();
            kernel.Bind<IDeviceRepository, ISimpleRepository<Device>>().To<DeviceRepository>();
            kernel.Bind<IDeviceNotificationRepository, ISimpleRepository<DeviceNotification>>().To<DeviceNotificationRepository>();
            kernel.Bind<IDeviceCommandRepository, ISimpleRepository<DeviceCommand>>().To<DeviceCommandRepository>();
            kernel.Bind<IDeviceEquipmentRepository, ISimpleRepository<DeviceEquipment>>().To<DeviceEquipmentRepository>();

            // bind JSON mapper
            kernel.Bind<JsonMapperManager>().ToSelf().InSingletonScope().OnActivation(JsonMapperConfig.ConfigureMapping);

            // bind data context
            kernel.Bind<DataContext>().ToSelf().InSingletonScope();

            // bind request context
            kernel.Bind<RequestContext>().ToSelf().InRequestScope();

            // bind object waiters
            kernel.Bind<ObjectWaiter<DeviceNotification>>().ToSelf().InSingletonScope();
            kernel.Bind<ObjectWaiter<DeviceCommand>>().ToSelf().InSingletonScope();

            // bind notification handlers
            kernel.Bind<INotificationManager>().To<NotificationManager>().InSingletonScope();
            kernel.Bind<INotificationHandler>().To<DeviceStatusNotificationHandler>();
            kernel.Bind<INotificationHandler>().To<EquipmentNotificationHandler>();

            // bind API XML reader
            kernel.Bind<XmlCommentReader>().ToSelf().InSingletonScope().Named("Data").WithConstructorArgument("fileName", "DeviceHive.Data.xml");
            kernel.Bind<XmlCommentReader>().ToSelf().InSingletonScope().Named("API").WithConstructorArgument("fileName", "DeviceHive.API.xml");
        }
    }
}
