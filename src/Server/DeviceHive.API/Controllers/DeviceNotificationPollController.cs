using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DeviceHive.API.Business;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="DeviceNotification" />
    public class DeviceNotificationPollController : BaseController
    {
        private static readonly int _defaultWaitTimeout = 30;
        private static readonly int _maxWaitTimeout = 60;

        private ITimestampRepository _timestampRepository;
        private ObjectWaiter _notificationByDeviceIdWaiter;

        public DeviceNotificationPollController(ITimestampRepository timestampRepository,
            [Named("DeviceNotification.DeviceID")] ObjectWaiter notificationByDeviceIdWaiter)
        {
            _timestampRepository = timestampRepository;
            _notificationByDeviceIdWaiter = notificationByDeviceIdWaiter;
        }

        /// <name>poll</name>
        /// <summary>
        ///     <para>Polls new device notifications.</para>
        ///     <para>This method returns all device notifications that were created after specified timestamp.</para>
        ///     <para>In the case when no notifications were found, the method blocks until new notification is received.
        ///         If no notifications are received within the waitTimeout period, the server returns an empty response.
        ///         In this case, to continue polling, the client should repeat the call with the same timestamp value.
        ///     </para>
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="timestamp">Timestamp of the last received notification (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns cref="DeviceNotification">If successful, this method returns array of <see cref="DeviceNotification"/> resources in the response body.</returns>
        [AuthorizeUser]
        public JArray Get(Guid deviceGuid, DateTime? timestamp = null, int? waitTimeout = null)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var start = timestamp != null ? timestamp.Value.AddTicks(10) : _timestampRepository.GetCurrentTimestamp();
            if (waitTimeout <= 0)
            {
                var filter = new DeviceNotificationFilter { Start = start };
                var notifications = DataContext.DeviceNotification.GetByDevice(device.ID, filter);
                return new JArray(notifications.Select(n => Mapper.Map(n)));
            }

            var waitUntil = DateTime.UtcNow.AddSeconds(Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            while (true)
            {
                using (var waiterHandle = _notificationByDeviceIdWaiter.BeginWait(device.ID))
                {
                    var filter = new DeviceNotificationFilter { Start = start };
                    var notifications = DataContext.DeviceNotification.GetByDevice(device.ID, filter);
                    if (notifications != null && notifications.Any())
                        return new JArray(notifications.Select(n => Mapper.Map(n)));

                    var now = DateTime.UtcNow;
                    if (now >= waitUntil || !waiterHandle.Handle.WaitOne(waitUntil - now))
                        return new JArray();
                }
            }
        }

        /// <name>pollMany</name>
        /// <summary>
        ///     <para>Polls new device notifications.</para>
        ///     <para>This method returns all device notifications that were created after specified timestamp.</para>
        ///     <para>In the case when no notifications were found, the method blocks until new notification is received.
        ///         If no notifications are received within the waitTimeout period, the server returns an empty response.
        ///         In this case, to continue polling, the client should repeat the call with the same timestamp value.
        ///     </para>
        /// </summary>
        /// <param name="deviceGuids">Comma-separated list of device unique identifiers.</param>
        /// <param name="timestamp">Timestamp of the last received notification (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns>If successful, this method returns array of the following resources in the response body.</returns>
        /// <response>
        ///     <parameter name="deviceGuid" type="guid">Associated device unique identifier.</parameter>
        ///     <parameter name="notification" cref="DeviceNotification">DeviceNotification resource.</parameter>
        /// </response>
        [AuthorizeUser]
        public JArray Get(string deviceGuids = null, DateTime? timestamp = null, int? waitTimeout = null)
        {
            var deviceIds = deviceGuids == null ? null : ParseDeviceGuids(deviceGuids).Select(deviceGuid =>
                {
                    var device = DataContext.Device.Get(deviceGuid);
                    if (device == null || !IsNetworkAccessible(device.NetworkID))
                        ThrowHttpResponse(HttpStatusCode.BadRequest, "Invalid deviceGuid: " + deviceGuid);

                    return device.ID;
                }).ToArray();

            var start = timestamp != null ? timestamp.Value.AddTicks(10) : _timestampRepository.GetCurrentTimestamp();
            if (waitTimeout <= 0)
            {
                var filter = new DeviceNotificationFilter { Start = start };
                var notifications = DataContext.DeviceNotification.GetByDevices(deviceIds, filter);
                return MapDeviceNotifications(notifications.Where(n => IsNetworkAccessible(n.Device.NetworkID)));
            }

            var waitUntil = DateTime.UtcNow.AddSeconds(Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            while (true)
            {
                using (var waiterHandle = _notificationByDeviceIdWaiter.BeginWait(
                    deviceIds == null ? new object[] { null } : deviceIds.Cast<object>().ToArray()))
                {
                    var filter = new DeviceNotificationFilter { Start = start };
                    var notifications = DataContext.DeviceNotification.GetByDevices(deviceIds, filter)
                        .Where(n => IsNetworkAccessible(n.Device.NetworkID)).ToArray();
                    if (notifications != null && notifications.Any())
                        return MapDeviceNotifications(notifications);

                    var now = DateTime.UtcNow;
                    if (now >= waitUntil || !waiterHandle.Handle.WaitOne(waitUntil - now))
                        return new JArray();
                }
            }
        }

        private JArray MapDeviceNotifications(IEnumerable<DeviceNotification> notifications)
        {
            return new JArray(notifications.Select(n =>
                {
                    return new JObject(
                        new JProperty("deviceGuid", n.Device.GUID),
                        new JProperty("notification", Mapper.Map(n)));
                }));
        }

        private Guid[] ParseDeviceGuids(string deviceGuids)
        {
            try
            {
                return deviceGuids.Split(',').Select(g => new Guid(g)).ToArray();
            }
            catch (FormatException)
            {
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Format of the deviceGuids parameter is invalid!");
                return null;
            }
        }

        private IJsonMapper<DeviceNotification> Mapper
        {
            get { return GetMapper<DeviceNotification>(); }
        }
    }
}