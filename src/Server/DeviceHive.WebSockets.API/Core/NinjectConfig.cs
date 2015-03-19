using System.Configuration;
using System.Reflection;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Core.Services;
using DeviceHive.Data;
using DeviceHive.Data.EF;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using DeviceHive.WebSockets.API.Controllers;
using DeviceHive.WebSockets.API.Subscriptions;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Network;
using DeviceHive.WebSockets.Core.Network.Fleck;
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
            // bind data context
            kernel.Bind<DeviceHive.Data.MongoDB.MongoConnection>().ToSelf().InSingletonScope();
            kernel.Bind<DataContext>().ToSelf().InSingletonScope()
                .OnActivation<DataContext>(context => { context.SetRepositoryCreator(type => kernel.Get(type)); });

            // bind repositories
            var dataContext = kernel.Get<DataContext>();
            foreach (var interfaceType in dataContext.RegisteredInterfaces)
                kernel.Bind(interfaceType).To(dataContext.GetRepositoryType(interfaceType));
            foreach (var objectType in dataContext.RegisteredObjects)
                kernel.Bind(typeof(ISimpleRepository<>).MakeGenericType(objectType)).To(dataContext.GetRepositoryTypeFor(objectType));

            // bind configuration
            var configuration = (DeviceHiveConfiguration)ConfigurationManager.GetSection("deviceHive");
            if (configuration == null)
                throw new ConfigurationErrorsException("DeviceHive element is missing in the configuration file!");
            kernel.Bind<DeviceHiveConfiguration>().ToConstant(configuration);

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

            // bind message bus
            kernel.Bind<MessageBus>().To<TcpSocketMessageBus>().InSingletonScope()
                .OnActivation(MessageBusConfig.ConfigureMessageBus);

            // bind web socket server
            kernel.Bind<WebSocketServerBase>().To<FleckWebSocketServer>();

            // bind message logic manager
            kernel.Bind<IMessageManager>().To<MessageManager>().InSingletonScope().OnActivation(m => m.Initialize(kernel));
        }
    }
}
