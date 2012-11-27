using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DeviceHive.API.Business;
using DeviceHive.API.Filters;
using DeviceHive.API.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="DeviceNotification" />
    public class DeviceNotificationPollController : BaseController
    {
        private ObjectWaiter<DeviceNotification> _notificationWaiter;
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

        public DeviceNotificationPollController(ObjectWaiter<DeviceNotification> notificationWaiter)
        {
            _notificationWaiter = notificationWaiter;
        }

        /// <name>poll</name>
        /// <summary>
        ///     <para>Polls new device notifications.</para>
        ///     <para>This method returns all device notifications that were created after specified timestamp.</para>
        ///     <para>In the case when no notifications were found, the method blocks until new notification is received.
        ///         The blocking period is limited (currently 30 seconds), and the server returns empty response if no notifications are received.
        ///         In this case, to continue polling, the client should repeat the call with the same timestamp value.
        ///     </para>
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="timestamp">Timestamp of the last received notification (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <returns cref="DeviceNotification">If successful, this method returns array of <see cref="DeviceNotification"/> resources in the response body.</returns>
        [AuthorizeUser]
        public JArray Get(Guid deviceGuid, DateTime? timestamp = null)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var start = timestamp != null ? timestamp.Value.AddTicks(10) : DateTime.UtcNow;

            var result = _notificationWaiter.WaitForObjects(device.ID, () => DataContext.DeviceNotification.GetByDevice(device.ID, start, null), _timeout);
            return new JArray(result.Select(n => Mapper.Map(n)));
        }

        private IJsonMapper<DeviceNotification> Mapper
        {
            get { return GetMapper<DeviceNotification>(); }
        }
    }
}