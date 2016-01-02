using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Threading.Tasks;
using DeviceHive.API.Filters;
using DeviceHive.API.Internal;
using DeviceHive.Core;
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
        /// <param name="names">Comma-separated list of notification names.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns cref="DeviceNotification">If successful, this method returns array of <see cref="DeviceNotification"/> resources in the response body.</returns>
        [Route("device/{deviceGuid:deviceGuid}/notification/poll")]
        [AuthorizeUser(AccessKeyAction = "GetDeviceNotification")]
        public async Task<JArray> Get(string deviceGuid, DateTime? timestamp = null, string names = null, int? waitTimeout = null)
        {
            var device = GetDeviceEnsureAccess(deviceGuid);

            var start = timestamp ?? _timestampRepository.GetCurrentTimestamp();
            var notificationNames = names != null ? names.Split(',') : null;
            if (waitTimeout <= 0)
            {
                var filter = new DeviceNotificationFilter { Start = start, IsDateInclusive = false, Notifications = notificationNames };
                var notifications = DataContext.DeviceNotification.GetByDevice(device.ID, filter);
                return new JArray(notifications.Select(n => MapDeviceNotification(n, device)));
            }

            var config = DeviceHiveConfiguration.RestEndpoint;
            var delayTask = Task.Delay(1000 * Math.Min(config.NotificationPollMaxInterval, waitTimeout ?? config.NotificationPollDefaultInterval));
            using (var waiterHandle = _notificationByDeviceIdWaiter.BeginWait(new object[] { device.ID },
                notificationNames == null ? null : notificationNames.Cast<object>().ToArray()))
            {
                do
                {
                    var filter = new DeviceNotificationFilter { Start = start, IsDateInclusive = false, Notifications = notificationNames };
                    var notifications = DataContext.DeviceNotification.GetByDevice(device.ID, filter);
                    if (notifications != null && notifications.Any())
                        return new JArray(notifications.Select(n => MapDeviceNotification(n, device)));
                }
                while (await Task.WhenAny(waiterHandle.Wait(), delayTask) != delayTask);
            }

            return new JArray();
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
        /// <param name="names">Comma-separated list of notification names.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns cref="DeviceNotification">If successful, this method returns array of <see cref="DeviceNotification"/> resources in the response body.</returns>
        /// <response>
        ///     <parameter name="deviceGuid" type="guid">Associated device unique identifier.</parameter>
        /// </response>
        [Route("device/notification/poll")]
        [AuthorizeUser(AccessKeyAction = "GetDeviceNotification")]
        public async Task<JArray> GetMany(string deviceGuids = null, DateTime? timestamp = null, string names = null, int? waitTimeout = null)
        {
            var deviceIds = deviceGuids == null ? null : ParseDeviceGuids(deviceGuids).Select(d => d.ID).ToArray();

            var start = timestamp ?? _timestampRepository.GetCurrentTimestamp();
            var notificationNames = names != null ? names.Split(',') : null;
            if (waitTimeout <= 0)
            {
                var filter = new DeviceNotificationFilter { Start = start, IsDateInclusive = false, Notifications = notificationNames };
                var notifications = DataContext.DeviceNotification.GetByDevices(deviceIds, filter);
                return MapDeviceNotifications(notifications.Where(n => IsDeviceAccessible(n.Device)));
            }

            var config = DeviceHiveConfiguration.RestEndpoint;
            var delayTask = Task.Delay(1000 * Math.Min(config.NotificationPollMaxInterval, waitTimeout ?? config.NotificationPollDefaultInterval));
            using (var waiterHandle = _notificationByDeviceIdWaiter.BeginWait(
                deviceIds == null ? new object[] { null } : deviceIds.Cast<object>().ToArray(),
                notificationNames == null ? null : notificationNames.Cast<object>().ToArray()))
            {
                do
                {
                    var filter = new DeviceNotificationFilter { Start = start, IsDateInclusive = false, Notifications = notificationNames };
                    var notifications = DataContext.DeviceNotification.GetByDevices(deviceIds, filter)
                        .Where(n => IsDeviceAccessible(n.Device)).ToArray();
                    if (notifications != null && notifications.Any())
                        return MapDeviceNotifications(notifications);
                }
                while (await Task.WhenAny(waiterHandle.Wait(), delayTask) != delayTask);
            }

            return new JArray();
        }

        private JArray MapDeviceNotifications(IEnumerable<DeviceNotification> notifications)
        {
            bool is21Format = GetClientVersion() > new System.Version(2, 0);
            return new JArray(notifications.Select(n => is21Format ?
                MapDeviceNotification(n) :
                new JObject(
                    new JProperty("deviceGuid", n.Device.GUID),
                    new JProperty("notification", MapDeviceNotification(n)))));
        }

        private Device[] ParseDeviceGuids(string deviceGuids)
        {
            return DataContext.Device.GetMany(deviceGuids.Split(','))
                .Where(device => IsDeviceAccessible(device)).ToArray();
        }
    }
}