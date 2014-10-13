using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Threading.Tasks;
using DeviceHive.API.Business;
using DeviceHive.API.Filters;
using DeviceHive.Core;
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
        /// <param name="names">Comma-separated list of commands names.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns cref="DeviceCommand">If successful, this method returns array of <see cref="DeviceCommand"/> resources in the response body.</returns>
        [Route("device/{deviceGuid:deviceGuid}/command/poll")]
        [AuthorizeUserOrDevice(AccessKeyAction = "GetDeviceCommand")]
        public async Task<JArray> Get(string deviceGuid, DateTime? timestamp = null, string names = null, int? waitTimeout = null) 
        {
            EnsureDeviceAccess(deviceGuid);

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var start = timestamp ?? _timestampRepository.GetCurrentTimestamp();
            var commandNames = names != null ? names.Split(',') : null;
            if (waitTimeout <= 0)
            {
                var filter = new DeviceCommandFilter { Start = start, IsDateInclusive = false, Commands = commandNames };
                var commands = DataContext.DeviceCommand.GetByDevice(device.ID, filter);
                return new JArray(commands.Select(n => Mapper.Map(n)));
            }

            var config = DeviceHiveConfiguration.RestEndpoint;
            var delayTask = Task.Delay(1000 * Math.Min(config.CommandPollMaxInterval, waitTimeout ?? config.CommandPollDefaultInterval));
            using (var waiterHandle = _commandByDeviceIdWaiter.BeginWait(device.ID))
            {
                do
                {
                    var filter = new DeviceCommandFilter { Start = start, IsDateInclusive = false, Commands = commandNames };
                    var commands = DataContext.DeviceCommand.GetByDevice(device.ID, filter);
                    if (commands != null && commands.Any())
                        return new JArray(commands.Select(n => Mapper.Map(n)));
                }
                while (await Task.WhenAny(waiterHandle.Wait(), delayTask) != delayTask);
            }

            return new JArray();
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
        /// <param name="names">Comma-separated list of commands names.</param>
        /// <param name="waitTimeout">Waiting timeout in seconds (default: 30 seconds, maximum: 60 seconds). Specify 0 to disable waiting.</param>
        /// <returns>If successful, this method returns array of the following resources in the response body.</returns>
        /// <response>
        ///     <parameter name="deviceGuid" type="guid">Associated device unique identifier.</parameter>
        ///     <parameter name="command" cref="DeviceCommand"><see cref="DeviceCommand"/> resource.</parameter>
        /// </response>
        [Route("device/command/poll")]
        [AuthorizeUser(AccessKeyAction = "GetDeviceCommand")]
        public async Task<JArray> GetMany(string deviceGuids = null, DateTime? timestamp = null, string names = null, int? waitTimeout = null)
        {
            var deviceIds = deviceGuids == null ? null : ParseDeviceGuids(deviceGuids).Select(deviceGuid =>
                {
                    var device = DataContext.Device.Get(deviceGuid);
                    if (device == null || !IsDeviceAccessible(device))
                        ThrowHttpResponse(HttpStatusCode.BadRequest, "Invalid deviceGuid: " + deviceGuid);

                    return device.ID;
                }).ToArray();

            var start = timestamp ?? _timestampRepository.GetCurrentTimestamp();
            var commandNames = names != null ? names.Split(',') : null;
            if (waitTimeout <= 0)
            {
                var filter = new DeviceCommandFilter { Start = start, IsDateInclusive = false, Commands = commandNames };
                var commands = DataContext.DeviceCommand.GetByDevices(deviceIds, filter);
                return MapDeviceCommands(commands.Where(c => IsDeviceAccessible(c.Device)));
            }

            var config = DeviceHiveConfiguration.RestEndpoint;
            var delayTask = Task.Delay(1000 * Math.Min(config.CommandPollMaxInterval, waitTimeout ?? config.CommandPollDefaultInterval));
            using (var waiterHandle = _commandByDeviceIdWaiter.BeginWait(
                deviceIds == null ? new object[] { null } : deviceIds.Cast<object>().ToArray()))
            {
                do
                {
                    var filter = new DeviceCommandFilter { Start = start, IsDateInclusive = false, Commands = commandNames };
                    var commands = DataContext.DeviceCommand.GetByDevices(deviceIds, filter)
                        .Where(c => IsDeviceAccessible(c.Device)).ToArray();
                    if (commands != null && commands.Any())
                        return MapDeviceCommands(commands);
                }
                while (await Task.WhenAny(waiterHandle.Wait(), delayTask) != delayTask);
            }

            return new JArray();
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
        [Route("device/{deviceGuid:deviceGuid}/command/{id:int}/poll")]
        [AuthorizeUser(AccessKeyAction = "GetDeviceCommand")]
        public async Task<JObject> Get(string deviceGuid, int id, int? waitTimeout = null)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var command = DataContext.DeviceCommand.Get(id);
            if (command == null || command.DeviceID != device.ID)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device command not found!");

            if (command.Status != null)
                return Mapper.Map(command);

            if (waitTimeout <= 0)
                return null;

            var config = DeviceHiveConfiguration.RestEndpoint;
            var delayTask = Task.Delay(1000 * Math.Min(config.CommandPollMaxInterval, waitTimeout ?? config.CommandPollDefaultInterval));
            using (var waiterHandle = _commandByCommandIdWaiter.BeginWait(id))
            {
                do
                {
                    command = DataContext.DeviceCommand.Get(id);
                    if (command != null && command.Status != null)
                        return Mapper.Map(command);
                }
                while (await Task.WhenAny(waiterHandle.Wait(), delayTask) != delayTask);
            }

            return null;
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

        private string[] ParseDeviceGuids(string deviceGuids)
        {
            return deviceGuids.Split(',').ToArray();
        }

        private IJsonMapper<DeviceCommand> Mapper
        {
            get { return GetMapper<DeviceCommand>(); }
        }
    }
}