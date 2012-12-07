using System;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Test.WebSocketsTest
{
    public class DeviceControllerTest : WebSocketTestBase
    {        
        #region Authenticate

        [Test]
        public void Authenticate_InvalidDevice()
        {
            var connection = DeviceController.Connect();
            var msg = DeviceController.Authenticate(connection, Guid.NewGuid(), "bbb");
            Expect((string) msg["status"], EqualTo("error"));
        }

        [Test]
        public void Authenticate_ValidDevice()
        {
            var connection = DeviceController.Connect();
            var msg = DeviceController.Authenticate(connection, DeviceGUID, DeviceKey);
            Expect((string) msg["status"], EqualTo("success"));
        }        

        #endregion


        #region Update device command

        [Test]
        public void UpdateDeviceCommand_UnauthRequest()
        {
            // create command
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var insertCommandRes = ClientController.InsertDeviceCommand(clientConnection,
                DeviceGUID, new JObject(new JProperty("command", "_ut")));
            var command = (JObject)insertCommandRes["command"];
            var commandId = (int)command["id"];

            // update command
            var connection = DeviceController.Connect();
            var msg = DeviceController.UpdateDeviceCommand(connection, commandId, new JObject(
                new JProperty("result", "testResult")));
            Expect((string)msg["status"], EqualTo("error"));
        }

        [Test]
        public void UpdateDeviceCommand_InvalidCommand()
        {
            var connection = DeviceController.Connect();
            DeviceController.Authenticate(connection, DeviceGUID, DeviceKey);
            var msg = DeviceController.UpdateDeviceCommand(connection, -1, new JObject(
                new JProperty("result", "testResult")));
            Expect((string)msg["status"], EqualTo("error"));
        }

        [Test]
        public void UpdateDeviceCommand_ValidRequest()
        {
            // create command
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var insertCommandRes = ClientController.InsertDeviceCommand(clientConnection,
                DeviceGUID, new JObject(new JProperty("command", "_ut")));
            var command = (JObject) insertCommandRes["command"];
            var commandId = (int) command["id"];

            // send handler for command update notification
            string result = null;
            clientConnection.SendMessageHandler = m =>
            {
                var res = JObject.Parse(m);
                var cmd = (JObject) res["command"];
                result = (string) cmd["result"];
            };

            // update command
            var connection = DeviceController.Connect();
            DeviceController.Authenticate(connection, DeviceGUID, DeviceKey);
            var msg = DeviceController.UpdateDeviceCommand(connection, commandId, new JObject(
                new JProperty("result", "testResult")));
            command = (JObject) msg["command"];
            
            Expect((string) msg["status"], EqualTo("success"));
            Expect((string) command["result"], EqualTo("testResult"));
            Expect(result, EqualTo("testResult"));
        }

        [Test]
        public void UpdateDeviceCommand_GatewayAuth()
        {
            // create command
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var insertCommandRes = ClientController.InsertDeviceCommand(clientConnection,
                DeviceGUID, new JObject(new JProperty("command", "_ut")));
            var command = (JObject)insertCommandRes["command"];
            var commandId = (int)command["id"];

            // send handler for command update notification
            string result = null;
            clientConnection.SendMessageHandler = m =>
            {
                var res = JObject.Parse(m);
                var cmd = (JObject)res["command"];
                result = (string)cmd["result"];
            };

            // update command
            var connection = DeviceController.Connect();
            var msg = DeviceController.UpdateDeviceCommand(connection, DeviceGUID, DeviceKey,
                commandId, new JObject(new JProperty("result", "testResult")));
            command = (JObject)msg["command"];

            Expect((string)msg["status"], EqualTo("success"));
            Expect((string)command["result"], EqualTo("testResult"));
            Expect(result, EqualTo("testResult"));
        }

        #endregion


        #region Insert device notification

        [Test]
        public void InsertDeviceNotification_EmptyData()
        {
            var connection = DeviceController.Connect();
            DeviceController.Authenticate(connection, DeviceGUID, DeviceKey);
            var msg = DeviceController.InsertDeviceNotification(connection, null);
            Expect((string) msg["status"], EqualTo("error"));
        }

        [Test]
        public void InsertDeviceNotification_ValidRequest()
        {
            var connection = DeviceController.Connect();
            DeviceController.Authenticate(connection, DeviceGUID, DeviceKey);
            var msg = DeviceController.InsertDeviceNotification(connection,
                new JObject(new JProperty("notification", "_ut")));
            Expect((string) msg["status"], EqualTo("success"));
        }

        #endregion
    }
}