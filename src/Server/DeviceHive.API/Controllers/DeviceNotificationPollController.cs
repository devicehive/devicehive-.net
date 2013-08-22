using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
        [AuthorizeUser(AccessKeyAction = "GetDeviceNotification")]
        public Task<JArray> Get(Guid deviceGuid, DateTime? timestamp = null, int? waitTimeout = null)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var taskSource = new TaskCompletionSource<JArray>();
            var start = timestamp ?? _timestampRepository.GetCurrentTimestamp();
            if (waitTimeout <= 0)
            {
                var filter = new DeviceNotificationFilter { Start = start, IsDateInclusive = false };
                var notifications = DataContext.DeviceNotification.GetByDevice(device.ID, filter);
                taskSource.SetResult(new JArray(notifications.Select(n => Mapper.Map(n))));
                return taskSource.Task;
            }

            var delayTask = Delay(1000 * Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            var waiterHandle = _notificationByDeviceIdWaiter.BeginWait(device.ID);
            taskSource.Task.ContinueWith(t => waiterHandle.Dispose());

            Action<bool> wait = null;
            Action<bool> wait2 = cancel =>
                {
                    if (cancel)
                    {
                        taskSource.SetResult(new JArray());
                        return;
                    }

                    var filter = new DeviceNotificationFilter { Start = start, IsDateInclusive = false };
                    var notifications = DataContext.DeviceNotification.GetByDevice(device.ID, filter);
                    if (notifications != null && notifications.Any())
                    {
                        taskSource.SetResult(new JArray(notifications.Select(n => Mapper.Map(n))));
                        return;
                    }

                    Task.Factory.ContinueWhenAny(new[] { waiterHandle.Wait(), delayTask }, t => wait(t == delayTask));
                };
            wait = wait2;
            wait(false);

            return taskSource.Task;
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
        ///     <parameter name="notification" cref="DeviceNotification"><see cref="DeviceNotification"/> resource.</parameter>
        /// </response>
        [AuthorizeUser(AccessKeyAction = "GetDeviceNotification")]
        public Task<JArray> Get(string deviceGuids = null, DateTime? timestamp = null, int? waitTimeout = null)
        {
            var deviceIds = deviceGuids == null ? null : ParseDeviceGuids(deviceGuids).Select(deviceGuid =>
                {
                    var device = DataContext.Device.Get(deviceGuid);
                    if (device == null || !IsDeviceAccessible(device))
                        ThrowHttpResponse(HttpStatusCode.BadRequest, "Invalid deviceGuid: " + deviceGuid);

                    return device.ID;
                }).ToArray();

            var taskSource = new TaskCompletionSource<JArray>();
            var start = timestamp ?? _timestampRepository.GetCurrentTimestamp();
            if (waitTimeout <= 0)
            {
                var filter = new DeviceNotificationFilter { Start = start, IsDateInclusive = false };
                var notifications = DataContext.DeviceNotification.GetByDevices(deviceIds, filter);
                taskSource.SetResult(MapDeviceNotifications(notifications.Where(n => IsDeviceAccessible(n.Device))));
                return taskSource.Task;
            }

            var delayTask = Delay(1000 * Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            var waiterHandle = _notificationByDeviceIdWaiter.BeginWait(
                deviceIds == null ? new object[] { null } : deviceIds.Cast<object>().ToArray());
            taskSource.Task.ContinueWith(t => waiterHandle.Dispose());

            Action<bool> wait = null;
            Action<bool> wait2 = cancel =>
            {
                if (cancel)
                {
                    taskSource.SetResult(new JArray());
                    return;
                }

                var filter = new DeviceNotificationFilter { Start = start, IsDateInclusive = false };
                var notifications = DataContext.DeviceNotification.GetByDevices(deviceIds, filter)
                    .Where(n => IsDeviceAccessible(n.Device)).ToArray();
                if (notifications != null && notifications.Any())
                {
                    taskSource.SetResult(MapDeviceNotifications(notifications));
                    return;
                }

                Task.Factory.ContinueWhenAny(new[] { waiterHandle.Wait(), delayTask }, t => wait(t == delayTask));
            };
            wait = wait2;
            wait(false);

            return taskSource.Task;
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