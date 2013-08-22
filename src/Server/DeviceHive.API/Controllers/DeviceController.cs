using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Core.Services;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="Device" />
    public class DeviceController : BaseController
    {
        private readonly MessageBus _messageBus;
        private readonly DeviceService _deviceService;

        public DeviceController(MessageBus messageBus, DeviceService deviceService)
        {
            _messageBus = messageBus;
            _deviceService = deviceService;
        }

        /// <name>list</name>
        /// <summary>
        /// Gets list of devices.
        /// </summary>
        /// <query cref="DeviceFilter" />
        /// <returns cref="Device">If successful, this method returns array of <see cref="Device"/> resources in the response body.</returns>
        [AuthorizeUser(AccessKeyAction = "GetDevice")]
        public JArray Get()
        {
            var devices = (List<Device>)null;
            var filter = MapObjectFromQuery<DeviceFilter>();

            if (RequestContext.CurrentUser.Role == (int)UserRole.Administrator)
            {
                // administrators get all devices
                devices = DataContext.Device.GetAll(filter);
            }
            else
            {
                // users see a limited set of devices
                devices = DataContext.Device.GetByUser(RequestContext.CurrentUser.ID, filter);
                if (RequestContext.CurrentUserPermissions != null)
                {
                    // if access key was used, limit devices to allowed ones
                    devices = devices.Where(d => RequestContext.CurrentUserPermissions.Any(p =>
                        p.IsNetworkAllowed(d.NetworkID.Value) && p.IsDeviceAllowed(d.GUID.ToString()))).ToList();
                }
            }

            return new JArray(devices.Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        /// <returns cref="Device">If successful, this method returns a <see cref="Device"/> resource in the response body.</returns>
        [AuthorizeUserOrDevice(AccessKeyAction = "GetDevice")]
        public JObject Get(Guid id)
        {
            EnsureDeviceAccess(id);

            var device = DataContext.Device.Get(id);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            return Mapper.Map(device);
        }

        public HttpResponseMessage Post(JObject json)
        {
            return HttpResponse(HttpStatusCode.MethodNotAllowed, "The method is not allowed, please use PUT /device/{id} to register a device");
        }

        /// <name>register</name>
        /// <summary>
        ///     <para>Registers a device.</para>
        ///     <para>If device with specified identifier has already been registered, it gets updated in case when valid key is provided in the authorization header.</para>
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        /// <param name="json" cref="Device">In the request body, supply a <see cref="Device"/> resource.</param>
        /// <request>
        ///     <parameter name="network" mode="remove" />
        ///     <parameter name="deviceClass" mode="remove" />
        ///     <parameter name="network" type="integer or object" required="false">
        ///         <para>Network identifier or <see cref="Network"/> object.</para>
        ///         <para>If object is passed, the target network will be searched by name and automatically created if not found.</para>
        ///         <para>In case when existing network is protected with the key, the key value must be included.</para>
        ///     </parameter>
        ///     <parameter name="deviceClass" type="integer or object" required="true">
        ///         <para>Device class identifier or <see cref="DeviceClass"/> object.</para>
        ///         <para>If object is passed, the target device class will be searched by name and version, and automatically created if not found.</para>
        ///         <para>The device class object will be also updated accordingly unless the DeviceClass.IsPermanent flag is set.</para>
        ///     </parameter>
        ///     <parameter name="equipment" type="array" required="false" cref="Equipment">
        ///         <para>Array of <see cref="Equipment"/> objects to be associated with the device class. If specified, all existing values will be replaced.</para>
        ///         <para>In case when device class is permanent, this value is ignored.</para>
        ///     </parameter>
        /// </request>
        [HttpNoContentResponse]
        [AuthorizeDeviceRegistration(AccessKeyAction = "RegisterDevice")]
        public void Put(Guid id, JObject json)
        {
            // get device as stored in the AuthorizeDeviceRegistration filter
            var device = Request.Properties.ContainsKey("Device") ? (Device)Request.Properties["Device"] : new Device(id);
            if (device.ID > 0 && !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized");

            try
            {
                var verifyNetworkKey = RequestContext.CurrentUser == null;
                _deviceService.SaveDevice(device, json, verifyNetworkKey, IsNetworkAccessible);
            }
            catch (InvalidDataException e)
            {
                ThrowHttpResponse(HttpStatusCode.BadRequest, e.Message);
            }
            catch (UnauthroizedNetworkException e)
            {
                ThrowHttpResponse(HttpStatusCode.Forbidden, e.Message);
            }
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing device.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        [AuthorizeUser]
        [HttpNoContentResponse]
        public void Delete(Guid id)
        {
            var device = DataContext.Device.Get(id);
            if (device != null && IsDeviceAccessible(device))
            {
                DataContext.Device.Delete(device.ID);
            }
        }

        private IJsonMapper<Device> Mapper
        {
            get { return GetMapper<Device>(); }
        }        
    }
}