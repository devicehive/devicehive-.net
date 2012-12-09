using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="DeviceCommand" />
    public class DeviceCommandController : BaseController
    {
        private readonly MessageBus _messageBus;

        public DeviceCommandController(MessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        /// <name>query</name>
        /// <summary>
        /// Queries device commands.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="start">Command start date (inclusive, UTC).</param>
        /// <param name="end">Command end date (inclusive, UTC)</param>
        /// <returns cref="DeviceCommand">If successful, this method returns array of <see cref="DeviceCommand"/> resources in the response body.</returns>
        [AuthorizeDeviceOrUser]
        public JToken Get(Guid deviceGuid, DateTime? start = null, DateTime? end = null)
        {
            EnsureDeviceAccess(deviceGuid);

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            return new JArray(DataContext.DeviceCommand.GetByDevice(device.ID, start, end).Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device command.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="id">Command identifier.</param>
        /// <returns cref="DeviceCommand">If successful, this method returns a <see cref="DeviceCommand"/> resource in the response body.</returns>
        [AuthorizeDeviceOrUser]
        public JObject Get(Guid deviceGuid, int id)
        {
            EnsureDeviceAccess(deviceGuid);

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var command = DataContext.DeviceCommand.Get(id);
            if (command == null || command.DeviceID != device.ID)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device command not found!");

            return Mapper.Map(command);
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new device command.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="json" cref="DeviceCommand">In the request body, supply a <see cref="DeviceCommand"/> resource.</param>
        /// <returns cref="DeviceCommand">If successful, this method returns a <see cref="DeviceCommand"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="status" mode="remove" />
        ///     <parameter name="result" mode="remove" />
        /// </request>
        [AuthorizeUser]
        [HttpCreatedResponse]
        public JObject Post(Guid deviceGuid, JObject json)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var command = Mapper.Map(json);
            command.Device = device;
            Validate(command);

            DataContext.DeviceCommand.Save(command);
            _messageBus.Notify(new DeviceCommandAddedMessage(device.ID, command.ID));
            return Mapper.Map(command);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing device command.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="id">Device command identifier.</param>
        /// <param name="json" cref="DeviceCommand">In the request body, supply a <see cref="DeviceCommand"/> resource.</param>
        /// <returns cref="DeviceCommand">If successful, this method returns a <see cref="DeviceCommand"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="command" mode="remove" />
        ///     <parameter name="parameters" mode="remove" />
        ///     <parameter name="lifetime" mode="remove" />
        /// </request>
        [AuthorizeDeviceOrUser(Roles = "Administrator")]
        public JObject Put(Guid deviceGuid, int id, JObject json)
        {
            EnsureDeviceAccess(deviceGuid);

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var command = DataContext.DeviceCommand.Get(id);
            if (command == null || command.DeviceID != device.ID)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device command not found!");

            Mapper.Apply(command, json);
            command.Device = device;
            Validate(command);

            DataContext.DeviceCommand.Save(command);
            _messageBus.Notify(new DeviceCommandUpdatedMessage(device.ID, command.ID));
            return Mapper.Map(command);
        }

        private IJsonMapper<DeviceCommand> Mapper
        {
            get { return GetMapper<DeviceCommand>(); }
        }
    }
}