using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
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
    [RoutePrefix("device")]
    public class DeviceController : BaseController
    {
        private readonly DeviceService _deviceService;

        public DeviceController(DeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        /// <name>list</name>
        /// <summary>
        /// Gets list of devices.
        /// </summary>
        /// <query cref="DeviceFilter" />
        /// <returns cref="Device">If successful, this method returns array of <see cref="Device"/> resources in the response body.</returns>
        [Route, AuthorizeUser(AccessKeyAction = "GetDevice")]
        public JArray Get()
        {
            var filter = MapObjectFromQuery<DeviceFilter>();
            var devices = CallContext.CurrentUser.Role == (int)UserRole.Administrator ?
                DataContext.Device.GetAll(filter) :
                DataContext.Device.GetByUser(CallContext.CurrentUser.ID, filter);

            if (CallContext.CurrentUserPermissions != null)
            {
                // if access key was used, limit devices to allowed ones
                devices = devices.Where(d => CallContext.CurrentUserPermissions.Any(p =>
                    p.IsNetworkAllowed(d.NetworkID) && p.IsDeviceAllowed(d.GUID))).ToList();
            }

            return new JArray(devices.Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        /// <returns cref="Device">If successful, this method returns a <see cref="Device"/> resource in the response body.</returns>
        [Route("{id:deviceGuid}"), AuthorizeUserOrDevice(AccessKeyAction = "GetDevice")]
        public JObject Get(string id)
        {
            EnsureDeviceAccess(id);

            var device = DataContext.Device.Get(id);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            return Mapper.Map(device);
        }

        [Route]
        public HttpResponseMessage Post(JObject json)
        {
            return HttpResponse(HttpStatusCode.MethodNotAllowed, "The method is not allowed, please use PUT /device/{id} to register a device");
        }

        /// <name>register</name>
        /// <summary>Registers or updates a device.</summary>
        /// <param name="id">Device unique identifier.</param>
        /// <param name="json" cref="Device">In the request body, supply a <see cref="Device"/> resource.</param>
        /// <request>
        ///     <parameter name="network">
        ///         <para>A <see cref="Network"/> object which includes name property to match.</para>
        ///         <para>In case when the target network is protected with a key, the key value must also be included.</para>
        ///         <para>For test deployments, any non-existing networks are automatically created.</para>
        ///     </parameter>
        ///     <parameter name="deviceClass">
        ///         <para>A <see cref="DeviceClass"/> object which includes name and version properties to match.</para>
        ///         <para>The device class objects are automatically created/updated unless the DeviceClass.IsPermanent flag is set.</para>
        ///     </parameter>
        /// </request>
        [HttpNoContentResponse]
        [Route("{id:deviceGuid}"), AuthorizeDeviceRegistration(AccessKeyAction = "RegisterDevice")]
        public void Put(string id, JObject json)
        {
            // get device as stored in the AuthorizeDeviceRegistration filter
            var device = Request.Properties.ContainsKey("Device") ? (Device)Request.Properties["Device"] : new Device(id);
            if (device.ID > 0 && !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized");

            try
            {
                var verifyNetworkKey = CallContext.CurrentUser == null;
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

            // if new device registered by itself - set online timestamp
            if (CallContext.CurrentUser == null && !Request.Properties.ContainsKey("Device"))
                DataContext.Device.SetLastOnline(device.ID);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing device.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        [HttpNoContentResponse]
        [Route("{id:deviceGuid}"), AuthorizeUser]
        public void Delete(string id)
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