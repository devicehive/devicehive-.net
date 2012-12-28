using DeviceHive.Test.Stubs;
using DeviceHive.WebSockets.ActionsFramework;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.Test.WebSocketsTest.Utils
{
    public abstract class ControllerWrapperBase<TController> where TController : ControllerBase
    {
        private readonly StubWebSocketServer _server;
        private readonly ControllerBase _controller;
        private readonly string _path;

        protected ControllerWrapperBase(StubWebSocketServer server, IKernel kernel, string path)
        {
            _server = server;
            _controller = kernel.Get<TController>();
            _path = path;
        }

        public StubWebSocketConnection Connect()
        {
            return _server.Connect(_path);
        }

        protected JObject InvokeAction(StubWebSocketConnection connection, string actionName, JObject args)
        {
            JObject jsonMessage = null;
            connection.SendMessageHandler = msg => jsonMessage = JObject.Parse(msg);
            _controller.InvokeAction(connection, actionName, args);
            return jsonMessage;
        }
    }
}