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
        /// <returns cref="Device">If successful, this method returns array of <see cref="Device"/> resources in the response body.</returns>
        [AuthorizeUser(Roles = "Administrator")]
        public JArray Get()
        {
            return new JArray(DataContext.Device.GetAll().Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        /// <returns cref="Device">If successful, this method returns a <see cref="Device"/> resource in the response body.</returns>
        [AuthorizeDeviceOrUser]
        public JObject Get(Guid id)
        {
            EnsureDeviceAccess(id);

            var device = DataContext.Device.Get(id);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
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
        /// <returns cref="Device">If successful, this method returns a <see cref="Device"/> resource in the response body.</returns>
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
        ///         <para>The device class object will be also updated accordingly unless the <see cref="DeviceClass.IsPermanent"/> flag is set.</para>
        ///     </parameter>
        ///     <parameter name="equipment" type="array" required="false" cref="Equipment">
        ///         <para>Array of <see cref="Equipment"/> objects to be associated with the device class. If specified, all existing values will be replaced.</para>
        ///         <para>In case when device class is permanent, this value is ignored.</para>
        ///     </parameter>
        /// </request>
        public JObject Put(Guid id, JObject json)
        {
            // load device from repository
            var device = DataContext.Device.Get(id);
            if (device != null)
            {
                // if device exists, administrator or device authorization is required
                if ((RequestContext.CurrentUser == null || RequestContext.CurrentUser.Role != (int)UserRole.Administrator) &&
                    (RequestContext.CurrentDevice == null || RequestContext.CurrentDevice.GUID != id))
                {
                    ThrowHttpResponse(HttpStatusCode.Unauthorized,  "Not authorized");
                }
            }
            else
            {
                // otherwise, create new device
                device = new Device(id);
            }

            JObject result = null;

            try
            {
                result = _deviceService.SaveDevice(device, json,
                    RequestContext.CurrentUser == null);
            }
            catch (InvalidDataException e)
            {
                ThrowHttpResponse(HttpStatusCode.BadRequest, e.Message);
            }
            catch (UnauthroizedNetworkException e)
            {
                ThrowHttpResponse(HttpStatusCode.Forbidden, e.Message);
            }

            return result;
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing device.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        [HttpNoContentResponse]
        [AuthorizeUser(Roles = "Administrator")]
        public void Delete(Guid id)
        {
            var device = DataContext.Device.Get(id);
            if (device != null)
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