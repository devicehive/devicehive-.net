using System;
using System.Threading;
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
        }        

        #endregion


        #region Device notification subscriptions

        [Test]
        public void DeviceNotificationsSubscription_SubscribeToValidDevice()
        {
            // subscribe to notifications
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var msg = ClientController.SubscribeToDeviceNotifications(clientConnection, new[] {DeviceGUID});
            Expect((string) msg["status"], EqualTo("success"));

            // send handler for new notification
            JObject notification = null;
            clientConnection.SendMessageHandler = m =>
            {
                var res = JObject.Parse(m);
                notification = (JObject) res["notification"];
            };

            // insert notification
            var deviceConnection = DeviceController.Connect();
            DeviceController.Authenticate(deviceConnection, DeviceGUID, DeviceKey);
            msg = DeviceController.InsertDeviceNotification(deviceConnection, new JObject(
                new JProperty("notification", "_ut")));
            var insertedNotification = (JObject) msg["notification"];

            clientConnection.WaiteForSendMessage();

            Expect(() => (string)notification["notification"], EqualTo("_ut"));
            Expect(() => (int) notification["id"], EqualTo((int) insertedNotification["id"]));
        }

        [Test]
        public void DeviceNotificationsSubscription_SubscribeToInvalidDevice()
        {
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var msg = ClientController.SubscribeToDeviceNotifications(clientConnection, new[] {Guid.NewGuid()});
            Expect((string) msg["status"], EqualTo("error"));
        }

        [Test]
        public void DeviceNotificationsSubscription_SubscribeToAnotherDevice()
        {
            // subscribe to notifications
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var msg = ClientController.SubscribeToDeviceNotifications(clientConnection, new[] {OtherDeviceGUID});
            Expect((string) msg["status"], EqualTo("success"));

            // send handler for new notification
            JObject notification = null;
            clientConnection.SendMessageHandler = m =>
            {
                var res = JObject.Parse(m);
                notification = (JObject) res["notification"];
            };

            // insert notification
            var deviceConnection = DeviceController.Connect();
            DeviceController.Authenticate(deviceConnection, DeviceGUID, DeviceKey);
            DeviceController.InsertDeviceNotification(deviceConnection, new JObject(
                new JProperty("notification", "_ut")));

            Expect(notification, EqualTo(null));
        }

        [Test]
        public void DeviceNotificationsSubscription_SubscribeToAnyDevice()
        {
            // subscribe to notifications
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var msg = ClientController.SubscribeToDeviceNotifications(clientConnection);
            Expect((string) msg["status"], EqualTo("success"));

            // send handler for new notification
            JObject notification = null;
            clientConnection.SendMessageHandler = m =>
            {
                var res = JObject.Parse(m);
                notification = (JObject) res["notification"];
            };

            // insert notification
            var deviceConnection = DeviceController.Connect();
            DeviceController.Authenticate(deviceConnection, DeviceGUID, DeviceKey);
            msg = DeviceController.InsertDeviceNotification(deviceConnection, new JObject(
                new JProperty("notification", "_ut")));
            var insertedNotification = (JObject) msg["notification"];

            clientConnection.WaiteForSendMessage();

            Expect(() => (string) notification["notification"], EqualTo("_ut"));
            Expect(() => (int) notification["id"], EqualTo((int) insertedNotification["id"]));
        }

        [Test]
        public void DeviceNotificationsSubscription_Unsubscribe()
        {
            // subscribe to notifications
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var msg = ClientController.SubscribeToDeviceNotifications(clientConnection, new[] {DeviceGUID});
            Expect((string) msg["status"], EqualTo("success"));

            // unsubscribe from notifications
            msg = ClientController.UnsubscribeFromDeviceNotifications(clientConnection, new[] {DeviceGUID});
            Expect((string) msg["status"], EqualTo("success"));

            // send handler for new notification
            JObject notification = null;
            clientConnection.SendMessageHandler = m =>
            {
                var res = JObject.Parse(m);
                notification = (JObject) res["notification"];
            };

            // insert notification
            var deviceConnection = DeviceController.Connect();
            DeviceController.Authenticate(deviceConnection, DeviceGUID, DeviceKey);
            DeviceController.InsertDeviceNotification(deviceConnection, new JObject(
                new JProperty("notification", "_ut")));

            Expect(notification, EqualTo(null));
        }

        [Test]
        public void DeviceNotificationsSubscription_UnSubscribeFromAnotherDevice()
        {
            // subscribe to notifications
            var clientConnection = ClientController.Connect();
            ClientController.Authenticate(clientConnection, Login, Password);
            var msg = ClientController.SubscribeToDeviceNotifications(clientConnection, new[] {DeviceGUID});
            Expect((string) msg["status"], EqualTo("success"));

            // unsubscribe from notifications
            msg = ClientController.UnsubscribeFromDeviceNotifications(clientConnection, new[] {OtherDeviceGUID});
            Expect((string) msg["status"], EqualTo("success"));

            // send handler for new notification
            JObject notification = null;
            clientConnection.SendMessageHandler = m =>
            {
                var res = JObject.Parse(m);
                notification = (JObject) res["notification"];
            };

            // insert notification
            var deviceConnection = DeviceController.Connect();
            DeviceController.Authenticate(deviceConnection, DeviceGUID, DeviceKey);
            msg = DeviceController.InsertDeviceNotification(deviceConnection, new JObject(
                new JProperty("notification", "_ut")));
            var insertedNotification = (JObject) msg["notification"];

            clientConnection.WaiteForSendMessage();

            Expect(() => (string) notification["notification"], EqualTo("_ut"));
            Expect(() => (int) notification["id"], EqualTo((int) insertedNotification["id"]));
        }

        #endregion
    }
}