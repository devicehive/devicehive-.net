﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="DeviceCommand" />
    [RoutePrefix("device/{deviceGuid:deviceGuid}/command")]
    public class DeviceCommandController : BaseController
    {
        private readonly IMessageManager _messageManager;

        public DeviceCommandController(IMessageManager messageManager)
        {
            _messageManager = messageManager;
        }

        /// <name>query</name>
        /// <summary>
        /// Queries device commands.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <query cref="DeviceCommandFilter" />
        /// <returns cref="DeviceCommand">If successful, this method returns array of <see cref="DeviceCommand"/> resources in the response body.</returns>
        [Route, AuthorizeUserOrDevice(AccessKeyAction = "GetDeviceCommand")]
        public JToken Get(string deviceGuid)
        {
            var device = GetDeviceEnsureAccess(deviceGuid);

            var filter = MapObjectFromQuery<DeviceCommandFilter>();
            return new JArray(DataContext.DeviceCommand.GetByDevice(device.ID, filter).Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device command.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="id">Command identifier.</param>
        /// <returns cref="DeviceCommand">If successful, this method returns a <see cref="DeviceCommand"/> resource in the response body.</returns>
        [Route("{id:int}"), AuthorizeUserOrDevice(AccessKeyAction = "GetDeviceCommand")]
        public JObject Get(string deviceGuid, int id)
        {
            var device = GetDeviceEnsureAccess(deviceGuid);

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
        /// <returns cref="DeviceCommand" mode="OneWayOnly">If successful, this method returns a <see cref="DeviceCommand"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="status" mode="remove" />
        ///     <parameter name="result" mode="remove" />
        /// </request>
        [HttpCreatedResponse]
        [Route, AuthorizeUser(AccessKeyAction = "CreateDeviceCommand")]
        public JObject Post(string deviceGuid, JObject json)
        {
            var device = GetDeviceEnsureAccess(deviceGuid);

            var command = Mapper.Map(json);
            command.Device = device;
            command.UserID = CallContext.CurrentUser.ID;
            Validate(command);

            var context = new MessageHandlerContext(command, CallContext.CurrentUser);
            _messageManager.HandleCommand(context);

            return Mapper.Map(command, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing device command.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="id">Device command identifier.</param>
        /// <param name="json" cref="DeviceCommand">In the request body, supply a <see cref="DeviceCommand"/> resource.</param>
        /// <request>
        ///     <parameter name="command" mode="remove" />
        ///     <parameter name="parameters" mode="remove" />
        ///     <parameter name="lifetime" mode="remove" />
        /// </request>
        [HttpNoContentResponse]
        [Route("{id:int}"), AuthorizeUserOrDevice(AccessKeyAction = "UpdateDeviceCommand")]
        public void Put(string deviceGuid, int id, JObject json)
        {
            var device = GetDeviceEnsureAccess(deviceGuid);

            var command = DataContext.DeviceCommand.Get(id);
            if (command == null || command.DeviceID != device.ID)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device command not found!");

            Mapper.Apply(command, json);
            command.Device = device;
            Validate(command);

            var context = new MessageHandlerContext(command, CallContext.CurrentUser);
            _messageManager.HandleCommandUpdate(context);
        }

        private IJsonMapper<DeviceCommand> Mapper
        {
            get { return GetMapper<DeviceCommand>(); }
        }
    }
}