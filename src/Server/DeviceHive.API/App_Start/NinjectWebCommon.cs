using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using DeviceHive.API.Internal;
using DeviceHive.Core;
using DeviceHive.Core.Authentication;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Core.Services;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using DeviceHive.Data.MongoDB;
using DeviceHive.Data.Repositories;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Ninject;
using Ninject.Web.Common;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(DeviceHive.API.NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(DeviceHive.API.NinjectWebCommon), "Stop")]

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
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            // load assembly modules
            kernel.Load(Assembly.GetExecutingAssembly());

            // bind data context
            kernel.Bind<MongoConnection>().ToSelf().InSingletonScope();
            kernel.Bind<DataContext>().ToSelf().InSingletonScope()
                .OnActivation<DataContext>(context => { context.SetRepositoryCreator(type => kernel.Get(type)); });

            // bind repositories
            var dataContext = kernel.Get<DataContext>();
            foreach (var interfaceType in dataContext.RegisteredInterfaces)
                kernel.Bind(interfaceType).To(dataContext.GetRepositoryType(interfaceType));
            foreach (var objectType in dataContext.RegisteredObjects)
                kernel.Bind(typeof(ISimpleRepository<>).MakeGenericType(objectType)).To(dataContext.GetRepositoryTypeFor(objectType));

            // bind configuration
            var configuration = (DeviceHiveConfiguration)ConfigurationManager.GetSection("deviceHive") ?? new DeviceHiveConfiguration();
            kernel.Bind<DeviceHiveConfiguration>().ToConstant(configuration);

            // bind services
            kernel.Bind<DeviceService>().ToSelf();

            // bind json mapper
            kernel.Bind<JsonMapperManager>().ToSelf().InSingletonScope().OnActivation(JsonMapperConfig.ConfigureMapping);

            // bind message bus
            kernel.Bind<MessageBus>().To<TcpSocketMessageBus>().InSingletonScope().OnActivation(MessageBusConfig.ConfigureSubscriptions);

            // bind object waiters
            kernel.Bind<ObjectWaiter>().ToSelf().InSingletonScope().Named("DeviceNotification.DeviceID");
            kernel.Bind<ObjectWaiter>().ToSelf().InSingletonScope().Named("DeviceCommand.DeviceID");
            kernel.Bind<ObjectWaiter>().ToSelf().InSingletonScope().Named("DeviceCommand.CommandID");

            // bind authentication manager
            kernel.Bind<IAuthenticationManager>().To<AuthenticationManager>().InSingletonScope().OnActivation(m => m.Initialize(kernel));

            // bind message logic manager
            kernel.Bind<IMessageManager>().To<MessageManager>().InSingletonScope().OnActivation(m => m.Initialize(kernel));

            // bind request context
            kernel.Bind<CallContext>().ToSelf().InRequestScope();
        }
    }
}
