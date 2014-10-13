using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="DeviceNotification" />
    [RoutePrefix("device/{deviceGuid:deviceGuid}/notification")]
    public class DeviceNotificationController : BaseController
    {
        private readonly IMessageManager _messageManager;
        private readonly MessageBus _messageBus;

        public DeviceNotificationController(IMessageManager messageManager, MessageBus messageBus)
        {
            _messageManager = messageManager;
            _messageBus = messageBus;
        }

        /// <name>query</name>
        /// <summary>
        /// Queries device notifications.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <query cref="DeviceNotificationFilter" />
        /// <returns cref="DeviceNotification">If successful, this method returns array of <see cref="DeviceNotification"/> resources in the response body.</returns>
        [Route, AuthorizeUser(AccessKeyAction = "GetDeviceNotification")]
        public JToken Get(string deviceGuid)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var filter = MapObjectFromQuery<DeviceNotificationFilter>();
            return new JArray(DataContext.DeviceNotification.GetByDevice(device.ID, filter).Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device notification.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="id">Notification identifier.</param>
        /// <returns cref="DeviceNotification">If successful, this method returns a <see cref="DeviceNotification"/> resource in the response body.</returns>
        [Route("{id:int}"), AuthorizeUser(AccessKeyAction = "GetDeviceNotification")]
        public JObject Get(string deviceGuid, int id)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var notification = DataContext.DeviceNotification.Get(id);
            if (notification == null || notification.DeviceID != device.ID)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device notification not found!");

            return Mapper.Map(notification);
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new device notification.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="json" cref="DeviceNotification">In the request body, supply a <see cref="DeviceNotification"/> resource.</param>
        /// <returns cref="DeviceNotification" mode="OneWayOnly">If successful, this method returns a <see cref="DeviceNotification"/> resource in the response body.</returns>
        [HttpCreatedResponse]
        [Route, AuthorizeUserOrDevice(AccessKeyAction = "CreateDeviceNotification")]
        public JObject Post(string deviceGuid, JObject json)
        {
            EnsureDeviceAccess(deviceGuid);

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var notification = Mapper.Map(json);
            notification.Device = device;
            Validate(notification);

            DataContext.DeviceNotification.Save(notification);
            _messageManager.ProcessNotification(notification);
            _messageBus.Notify(new DeviceNotificationAddedMessage(device.ID, notification.ID));

            return Mapper.Map(notification, oneWayOnly: true);
        }

        private IJsonMapper<DeviceNotification> Mapper
        {
            get { return GetMapper<DeviceNotification>(); }
        }
    }
}