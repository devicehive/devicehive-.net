using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DeviceHive.API.Business;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="DeviceCommand" />
    public class DeviceCommandPollController : BaseController
    {
        private ObjectWaiter _commandWaiter;
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

        public DeviceCommandPollController([Named("DeviceCommand")] ObjectWaiter commandWaiter)
        {
            _commandWaiter = commandWaiter;
        }

        /// <name>poll</name>
        /// <summary>
        ///     <para>Polls new device commands.</para>
        ///     <para>This method returns all device commands that were created after specified timestamp.</para>
        ///     <para>In the case when no commands were found, the method blocks until new command is received.
        ///         The blocking period is limited (currently 30 seconds), and the server returns empty response if no commands are received.
        ///         In this case, to continue polling, the client should repeat the call with the same timestamp value.
        ///     </para>
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="timestamp">Timestamp of the last received command (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <returns cref="DeviceCommand">If successful, this method returns array of <see cref="DeviceCommand"/> resources in the response body.</returns>
        [AuthorizeDeviceOrUser]
        public JArray Get(Guid deviceGuid, DateTime? timestamp = null) 
        {
            EnsureDeviceAccess(deviceGuid);

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var waitUntil = DateTime.UtcNow.Add(_timeout);
            var start = timestamp != null ? timestamp.Value.AddTicks(10) : DataContext.DeviceCommand.GetCurrentTimestamp();

            while (true)
            {
                using (var waiterHandle = _commandWaiter.BeginWait(device.GUID))
                {
                    var commands = DataContext.DeviceCommand.GetByDevice(device.ID, start, null);
                    if (commands != null && commands.Any())
                        return new JArray(commands.Select(n => Mapper.Map(n)));

                    var now = DateTime.UtcNow;
                    if (now >= waitUntil || !waiterHandle.Handle.WaitOne(waitUntil - now))
                        return new JArray();
                }
            }
        }

        private IJsonMapper<DeviceCommand> Mapper
        {
            get { return GetMapper<DeviceCommand>(); }
        }
    }
}