using System;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Test.WebSocketsTest
{
    [TestFixture]
    public class ClientControllerTest : WebSocketTestBase
    {        
        #region Authenticate

        [Test]
        public void Authenticate_InvalidUser()
        {
            var connection = ClientController.Connect();
            var msg = ClientController.Authenticate(connection, "aaa", "bbb");
            Expect((string)msg["status"], EqualTo("error"));
        }

        [Test]
        public void Authenticate_ValidUser()
        {
            var connection = ClientController.Connect();
            var msg = ClientController.Authenticate(connection, Login, Password);
            Expect((string) msg["status"], EqualTo("success"));
        }        

        #endregion


        #region Insert device command

        [Test]
        public void InsertDeviceCommand_UnauthRequest()
        {
            var connection = ClientController.Connect();
            var res = ClientController.InsertDeviceCommand(connection, DeviceGUID,
                new JObject(new JProperty("command", "_ut")));
            Expect((string)res["status"], EqualTo("error"));
        }

        [Test]
        public void InsertDeviceCommand_EmptyData()
        {
            var connection = ClientController.Connect();
            ClientController.Authenticate(connection, Login, Password);
            var res = ClientController.InsertDeviceCommand(connection, Guid.Empty, null);
            Expect((string) res["status"], EqualTo("error"));
        }

        [Test]
        public void InsertDeviceCommand_ValidData()
        {
            var connection = ClientController.Connect();
            ClientController.Authenticate(connection, Login, Password);
            var res = ClientController.InsertDeviceCommand(connection, DeviceGUID,
                new JObject(new JProperty("command", "_ut")));
            Expect((string) res["status"], EqualTo("success"));

            var resCommand = (JObject) res["command"];
            Expect((string) resCommand["command"], EqualTo("_ut"));
        }        

        #endregion


        #region Subscribe to device notifications



        #endregion
    }
}