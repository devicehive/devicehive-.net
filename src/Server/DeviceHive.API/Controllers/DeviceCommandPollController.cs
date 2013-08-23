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
    /// <resource cref="DeviceCommand" />
    public class DeviceCommandPollController : BaseController
    {
        private static readonly int _defaultWaitTimeout = 30;
        private static readonly int _maxWaitTimeout = 60;

        private ITimestampRepository _timestampRepository;
        private ObjectWaiter _commandByDeviceIdWaiter;
        private ObjectWaiter _commandByCommandIdWaiter;

        public DeviceCommandPollController(ITimestampRepository timestampRepository,
            [Named("DeviceCommand.DeviceID")] ObjectWaiter commandByDeviceIdWaiter,
            [Named("DeviceCommand.CommandID")] ObjectWaiter commandByCommandIdWaiter)
        {
            _timestampRepository = timestampRepository;
            _commandByDeviceIdWaiter = commandByDeviceIdWaiter;
            _commandByCommandIdWaiter = commandByCommandIdWaiter;
        }

        /// <name>poll</name>
        /// <summary>
        ///     <para>Polls new device commands.</para>
        ///     <para>This method returns all device commands that were created after specified timestamp.</para>
        ///     <para>In the case when no commands were found, the method blocks until new command is received.
        ///         If no commands are received within the waitTimeout period, the server returns an empty response.
        ///         In this case, to continue polling, the client should repeat the call with the same timestamp value.
        ///     </para>
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="timestamp">Timestamp of the last received command (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns cref="DeviceCommand">If successful, this method returns array of <see cref="DeviceCommand"/> resources in the response body.</returns>
        [AuthorizeUserOrDevice(AccessKeyAction = "GetDeviceCommand")]
        public Task<JArray> Get(Guid deviceGuid, DateTime? timestamp = null, int? waitTimeout = null) 
        {
            EnsureDeviceAccess(deviceGuid);

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var taskSource = new TaskCompletionSource<JArray>();
            var start = timestamp ?? _timestampRepository.GetCurrentTimestamp();
            if (waitTimeout <= 0)
            {
                var filter = new DeviceCommandFilter { Start = start, IsDateInclusive = false };
                var commands = DataContext.DeviceCommand.GetByDevice(device.ID, filter);
                taskSource.SetResult(new JArray(commands.Select(n => Mapper.Map(n))));
                return taskSource.Task;
            }

            var delayTask = Delay(1000 * Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            var waiterHandle = _commandByDeviceIdWaiter.BeginWait(device.ID);
            taskSource.Task.ContinueWith(t => waiterHandle.Dispose());

            Action<bool> wait = null;
            Action<bool> wait2 = cancel =>
                {
                    if (cancel)
                    {
                        taskSource.SetResult(new JArray());
                        return;
                    }

                    var filter = new DeviceCommandFilter { Start = start, IsDateInclusive = false };
                    var commands = DataContext.DeviceCommand.GetByDevice(device.ID, filter);
                    if (commands != null && commands.Any())
                    {
                        taskSource.SetResult(new JArray(commands.Select(n => Mapper.Map(n))));
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
        ///     <para>Polls new device commands.</para>
        ///     <para>This method returns all device commands that were created after specified timestamp.</para>
        ///     <para>In the case when no commands were found, the method blocks until new command is received.
        ///         If no commands are received within the waitTimeout period, the server returns an empty response.
        ///         In this case, to continue polling, the client should repeat the call with the same timestamp value.
        ///     </para>
        /// </summary>
        /// <param name="deviceGuids">Comma-separated list of device unique identifiers.</param>
        /// <param name="timestamp">Timestamp of the last received command (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns>If successful, this method returns array of the following resources in the response body.</returns>
        /// <response>
        ///     <parameter name="deviceGuid" type="guid">Associated device unique identifier.</parameter>
        ///     <parameter name="command" cref="DeviceCommand"><see cref="DeviceCommand"/> resource.</parameter>
        /// </response>
        [AuthorizeUser(AccessKeyAction = "GetDeviceCommand")]
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
                var filter = new DeviceCommandFilter { Start = start, IsDateInclusive = false };
                var commands = DataContext.DeviceCommand.GetByDevices(deviceIds, filter);
                taskSource.SetResult(MapDeviceCommands(commands.Where(c => IsDeviceAccessible(c.Device))));
                return taskSource.Task;
            }

            var delayTask = Delay(1000 * Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            var waiterHandle = _commandByDeviceIdWaiter.BeginWait(
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

                var filter = new DeviceCommandFilter { Start = start, IsDateInclusive = false };
                var commands = DataContext.DeviceCommand.GetByDevices(deviceIds, filter)
                    .Where(c => IsDeviceAccessible(c.Device)).ToArray();
                if (commands != null && commands.Any())
                {
                    taskSource.SetResult(MapDeviceCommands(commands));
                    return;
                }

                Task.Factory.ContinueWhenAny(new[] { waiterHandle.Wait(), delayTask }, t => wait(t == delayTask));
            };
            wait = wait2;
            wait(false);

            return taskSource.Task;
        }

        /// <name>wait</name>
        /// <summary>
        ///     <para>Waits for a command to be processed.</para>
        ///     <para>This method returns a command only if it has been processed by a device.</para>
        ///     <para>In the case when command is not processed, the method blocks until device acknowledges command execution.
        ///         If the command is not processed within the waitTimeout period, the server returns an empty response.
        ///         In this case, to continue polling, the client should repeat the call.
        ///     </para>
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="id">Command identifier.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns cref="DeviceCommand">If successful, this method returns a <see cref="DeviceCommand"/> resource in the response body.</returns>
        [AuthorizeUser(AccessKeyAction = "GetDeviceCommand")]
        public Task<JObject> Get(Guid deviceGuid, int id, int? waitTimeout = null)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var command = DataContext.DeviceCommand.Get(id);
            if (command == null || command.DeviceID != device.ID)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device command not found!");

            var taskSource = new TaskCompletionSource<JObject>();
            if (command.Status != null)
            {
                taskSource.SetResult(Mapper.Map(command));
                return taskSource.Task;
            }
            if (waitTimeout <= 0)
            {
                taskSource.SetResult(null);
                return taskSource.Task;
            }

            var delayTask = Delay(1000 * Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            var waiterHandle = _commandByCommandIdWaiter.BeginWait(id);
            taskSource.Task.ContinueWith(t => waiterHandle.Dispose());

            Action<bool> wait = null;
            Action<bool> wait2 = cancel =>
            {
                if (cancel)
                {
                    taskSource.SetResult(null);
                    return;
                }

                command = DataContext.DeviceCommand.Get(id);
                if (command != null && command.Status != null)
                {
                    taskSource.SetResult(Mapper.Map(command));
                    return;
                }

                Task.Factory.ContinueWhenAny(new[] { waiterHandle.Wait(), delayTask }, t => wait(t == delayTask));
            };
            wait = wait2;
            wait(false);

            return taskSource.Task;
        }

        private JArray MapDeviceCommands(IEnumerable<DeviceCommand> commands)
        {
            return new JArray(commands.Select(n =>
                {
                    return new JObject(
                        new JProperty("deviceGuid", n.Device.GUID),
                        new JProperty("command", Mapper.Map(n)));
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

        private IJsonMapper<DeviceCommand> Mapper
        {
            get { return GetMapper<DeviceCommand>(); }
        }
    }
}