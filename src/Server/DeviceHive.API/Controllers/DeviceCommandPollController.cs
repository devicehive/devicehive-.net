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
        [AuthorizeDeviceOrUser]
        public JArray Get(Guid deviceGuid, DateTime? timestamp = null, int? waitTimeout = null) 
        {
            EnsureDeviceAccess(deviceGuid);

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var start = timestamp != null ? timestamp.Value.AddTicks(10) : _timestampRepository.GetCurrentTimestamp();
            if (waitTimeout <= 0)
            {
                var filter = new DeviceCommandFilter { Start = start };
                var commands = DataContext.DeviceCommand.GetByDevice(device.ID, filter);
                return new JArray(commands.Select(n => Mapper.Map(n)));
            }

            var waitUntil = DateTime.UtcNow.AddSeconds(Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            while (true)
            {
                using (var waiterHandle = _commandByDeviceIdWaiter.BeginWait(device.ID))
                {
                    var filter = new DeviceCommandFilter { Start = start };
                    var commands = DataContext.DeviceCommand.GetByDevice(device.ID, filter);
                    if (commands != null && commands.Any())
                        return new JArray(commands.Select(n => Mapper.Map(n)));

                    var now = DateTime.UtcNow;
                    if (now >= waitUntil || !waiterHandle.Handle.WaitOne(waitUntil - now))
                        return new JArray();
                }
            }
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
        [AuthorizeUser]
        public JObject Get(Guid deviceGuid, int id, int? waitTimeout = null)
        {
            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var command = DataContext.DeviceCommand.Get(id);
            if (command == null || command.DeviceID != device.ID)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device command not found!");

            if (command.Status != null)
                return Mapper.Map(command);

            if (waitTimeout <= 0)
                return null;

            var waitUntil = DateTime.UtcNow.AddSeconds(Math.Min(_maxWaitTimeout, waitTimeout ?? _defaultWaitTimeout));
            while (true)
            {
                using (var waiterHandle = _commandByCommandIdWaiter.BeginWait(id))
                {
                    command = DataContext.DeviceCommand.Get(id);
                    if (command != null && command.Status != null)
                        return Mapper.Map(command);

                    var now = DateTime.UtcNow;
                    if (now >= waitUntil || !waiterHandle.Handle.WaitOne(waitUntil - now))
                        return null;
                }
            }
        }

        private IJsonMapper<DeviceCommand> Mapper
        {
            get { return GetMapper<DeviceCommand>(); }
        }
    }
}