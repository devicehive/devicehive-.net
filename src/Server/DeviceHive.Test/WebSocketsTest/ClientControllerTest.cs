using System;
using DeviceHive.Test.Stubs;
using DeviceHive.WebSockets.Controllers;
using DeviceHive.WebSockets.Network;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.Test.WebSocketsTest
{
    [TestFixture]
    public class ClientControllerTest : AssertionHelper
    {
        private static readonly Guid DeviceGUID = new Guid("a97266f4-6e8a-4008-8242-022b49ea484f");
        private const string Login = "dhadmin";
        private const string Password = "dhadmin_#911";

        private StubWebSocketServer _webSocketServer;
        private ClientController _clientController;

        [SetUp]
        public void SetUp()
        {
            var kernel = NinjectConfig.CreateKernel();
            _webSocketServer = (StubWebSocketServer) kernel.Get<WebSocketServerBase>();
            _clientController = kernel.Get<ClientController>();
        }


        #region Helper methods

        private StubWebSocketConnection Connect()
        {
            return _webSocketServer.Connect("/client");
        }

        private Action<string> CreateJsonMessageHandler(Action<JObject> messageHandler)
        {
            return msg => messageHandler(JObject.Parse(msg));
        }

        private JObject InvokeAction(StubWebSocketConnection connection, string actionName, JObject args)
        {
            JObject jsonMessage = null;
            connection.SendMessageHandler = CreateJsonMessageHandler(msg => jsonMessage = msg);
            _clientController.InvokeAction(connection, actionName, args);
            return jsonMessage;
        }

        #endregion


        #region Authenticate

        [Test]
        public void Authenticate_InvalidUser()
        {
            var connection = Connect();
            var msg = Authenticate(connection, "aaa", "bbb");
            Expect((string)msg["status"], EqualTo("error"));
        }

        [Test]
        public void Authenticate_ValidUser()
        {
            var connection = Connect();
            var msg = Authenticate(connection, Login, Password);
            Expect((string) msg["status"], EqualTo("success"));
        }

        private JObject Authenticate(StubWebSocketConnection connection, string login, string password)
        {
            return InvokeAction(connection, "authenticate", new JObject(
                new JProperty("login", login),
                new JProperty("password", password)));
        }

        #endregion


        #region Insert device command

        [Test]
        public void InsertDeviceCommand_UnauthRequest()
        {
            var connection = Connect();
            var res = InsertDeviceCommand(connection, DeviceGUID,
                new JObject(new JProperty("command", "_ut")));
            Expect((string)res["status"], EqualTo("error"));
        }

        [Test]
        public void InsertDeviceCommand_EmptyData()
        {
            var connection = Connect();
            Authenticate(connection, Login, Password);
            var res = InsertDeviceCommand(connection, Guid.Empty, null);
            Expect((string) res["status"], EqualTo("error"));
        }

        [Test]
        public void InsertDeviceCommand_ValidData()
        {
            var connection = Connect();
            Authenticate(connection, Login, Password);
            var res = InsertDeviceCommand(connection, DeviceGUID,
                new JObject(new JProperty("command", "_ut")));
            Expect((string) res["status"], EqualTo("success"));

            var resCommand = (JObject) res["command"];
            Expect((string) resCommand["command"], EqualTo("_ut"));
        }

        private JObject InsertDeviceCommand(StubWebSocketConnection connection,
            Guid deviceGuid, JObject command)
        {
            return InvokeAction(connection, "command/insert", new JObject(
                new JProperty("deviceGuid", deviceGuid),
                new JProperty("command", command)));
        }

        #endregion
    }
}