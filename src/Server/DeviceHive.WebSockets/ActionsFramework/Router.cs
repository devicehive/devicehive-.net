using System;
using System.Collections.Generic;
using DeviceHive.WebSockets.Network;
using Newtonsoft.Json.Linq;
using log4net;

namespace DeviceHive.WebSockets.ActionsFramework
{
    public abstract class Router
    {
        private readonly IDictionary<string, Type> _controllersMapping = new Dictionary<string, Type>();

        public void RegisterController(string path, Type type)
        {
            _controllersMapping.Add(path, type);
        }

        public void RouteRequest(WebSocketConnectionBase connection, string message)
        {
            try
            {
                var controller = GetController(connection);

                var request = JObject.Parse(message);
                var action = (string) request["action"];

                controller.InvokeAction(connection, action, request);
            }
            catch (Exception e)
            {
                LogManager.GetLogger(typeof(Router)).Error("WebSocket request error", e);
            }
        }

        public void CleanupConnection(WebSocketConnectionBase connection)
        {
            var controller = GetController(connection);
            controller.CleanupConnection(connection);
        }

        private ControllerBase GetController(WebSocketConnectionBase connection)
        {
            Type controllerType;
            if (!_controllersMapping.TryGetValue(connection.Path, out controllerType))
                throw new InvalidOperationException("Can't accept connections on invalid path: " + connection.Path);

            return (ControllerBase) CreateController(controllerType);
        }

        protected abstract object CreateController(Type type);
    }
}