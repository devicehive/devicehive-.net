using System.Reflection;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Data;
using DeviceHive.Data.Repositories;
using DeviceHive.WebSockets.API.Controllers;
using DeviceHive.WebSockets.API.Core;
using DeviceHive.WebSockets.API.Service;
using DeviceHive.WebSockets.API.Subscriptions;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Network;
using Ninject;

namespace DeviceHive.Test.WebSocketsTest.Utils
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
            kernel.Bind<DataContext>().ToSelf().InSingletonScope()
                .OnActivation<DataContext>(context => { context.SetRepositoryCreator(type => kernel.Get(type)); });

            // bind repositories
            var dataContext = kernel.Get<DataContext>();
            foreach (var interfaceType in dataContext.RegisteredInterfaces)
                kernel.Bind(interfaceType).To(dataContext.GetRepositoryType(interfaceType));
            foreach (var objectType in dataContext.RegisteredObjects)
                kernel.Bind(typeof(ISimpleRepository<>).MakeGenericType(objectType)).To(dataContext.GetRepositoryTypeFor(objectType));

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
            kernel.Bind<MessageBus>().To<Stubs.StubMessageBus>().InSingletonScope()
                .OnActivation(MessageBusConfig.ConfigureMessageBus);

            // bind subscription managers
            kernel.Bind<DeviceSubscriptionManager>().ToSelf().InSingletonScope().Named("DeviceCommand");
            kernel.Bind<DeviceSubscriptionManager>().ToSelf().InSingletonScope().Named("DeviceNotification");
            kernel.Bind<CommandSubscriptionManager>().ToSelf().InSingletonScope();

            // bind web socket server
            kernel.Bind<WebSocketServerBase>().To<Stubs.StubWebSocketServer>().InSingletonScope();
            kernel.Bind<SelfHostServiceImpl>().ToSelf().InSingletonScope();

            // bind message logic manager
            kernel.Bind<IMessageManager>().To<MessageManager>().InSingletonScope().OnActivation(m => m.Initialize(kernel));
        }
    }
}