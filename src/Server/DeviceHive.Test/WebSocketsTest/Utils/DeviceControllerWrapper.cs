﻿using System;
using DeviceHive.Test.Stubs;
using DeviceHive.WebSockets.API.Controllers;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.Test.WebSocketsTest.Utils
{
    public class DeviceControllerWrapper : ControllerWrapperBase<DeviceController>
    {
        public DeviceControllerWrapper(StubWebSocketServer server, IKernel kernel) :
            base(server, kernel, "/device")
        {
        }

        public JObject Authenticate(StubWebSocketConnection connection, string deviceId, string deviceKey)
        {
            return InvokeAction(connection, "authenticate", new JObject(
                new JProperty("deviceId", deviceId),
                new JProperty("deviceKey", deviceKey)));
        }

        public JObject UpdateDeviceCommand(StubWebSocketConnection connection, int commandId, JObject command)
        {
            return InvokeAction(connection, "command/update", new JObject(
                new JProperty("commandId", commandId),
                new JProperty("command", command)));
        }

        public JObject UpdateDeviceCommand(StubWebSocketConnection connection, string deviceId, string deviceKey,
            int commandId, JObject command)
        {
            return InvokeAction(connection, "command/update", new JObject(
                new JProperty("commandId", commandId),
                new JProperty("command", command),
                new JProperty("deviceId", deviceId),
                new JProperty("deviceKey", deviceKey)));
        }

        public JObject InsertDeviceNotification(StubWebSocketConnection connection, JObject notification)
        {
            return InvokeAction(connection, "notification/insert", new JObject(
                new JProperty("notification", notification)));
        }

        public JObject SubsrcibeToDeviceCommands(StubWebSocketConnection connection)
        {
            return InvokeAction(connection, "command/subscribe", null);
        }

        public JObject UnsubsrcibeFromDeviceCommands(StubWebSocketConnection connection)
        {
            return InvokeAction(connection, "command/unsubscribe", null);
        }

        public JObject GetDevice(StubWebSocketConnection connection)
        {
            return InvokeAction(connection, "device/get", null);
        }

        public JObject SaveDevice(StubWebSocketConnection connection, string deviceGuid, JObject device)
        {
            return InvokeAction(connection, "device/save", new JObject(
                new JProperty("deviceId", deviceGuid),
                new JProperty("device", device)));
        }
    }
}