using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="DeviceClass" />
    [RoutePrefix("device/class")]
    public class DeviceClassController : BaseController
    {
        /// <name>list</name>
        /// <summary>
        /// Gets list of device classes.
        /// </summary>
        /// <query cref="DeviceClassFilter" />
        /// <returns cref="DeviceClass">If successful, this method returns array of <see cref="DeviceClass"/> resources in the response body.</returns>
        [Route, AuthorizeAdmin(AccessKeyAction = "ManageDeviceClass")]
        public JArray Get()
        {
            var filter = MapObjectFromQuery<DeviceClassFilter>();
            return new JArray(DataContext.DeviceClass.GetAll(filter).Select(dc => Mapper.Map(dc)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device class and its equipment.
        /// </summary>
        /// <param name="id">Device class identifier.</param>
        /// <returns cref="DeviceClass">If successful, this method returns a <see cref="DeviceClass"/> resource in the response body.</returns>
        [Route("{id:int}"), AuthorizeUser(AccessKeyAction = "GetDevice")]
        public JObject Get(int id)
        {
            var deviceClass = DataContext.DeviceClass.Get(id);
            if (deviceClass == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device class not found!");

            return Mapper.Map(deviceClass);
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new device class.
        /// </summary>
        /// <param name="json" cref="DeviceClass">In the request body, supply a <see cref="DeviceClass"/> resource.</param>
        /// <returns cref="DeviceClass" mode="OneWayOnly">If successful, this method returns a <see cref="DeviceClass"/> resource in the response body.</returns>
        [HttpCreatedResponse]
        [Route, AuthorizeAdmin(AccessKeyAction = "ManageDeviceClass")]
        public JObject Post(JObject json)
        {
            var deviceClass = Mapper.Map(json);
            Validate(deviceClass);
            if (deviceClass.Equipment != null)
                deviceClass.Equipment.ForEach(e => Validate(e));

            if (DataContext.DeviceClass.Get(deviceClass.Name, deviceClass.Version) != null)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Device class with such name and version already exists!");

            DataContext.DeviceClass.Save(deviceClass);
            return Mapper.Map(deviceClass, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing device class.
        /// </summary>
        /// <param name="id">Device class identifier.</param>
        /// <param name="json" cref="DeviceClass">In the request body, supply a <see cref="DeviceClass"/> resource.</param>
        /// <request>
        ///     <parameter name="name" required="false" />
        ///     <parameter name="version" required="false" />
        ///     <parameter name="isPermanent" required="false" />
        ///     <parameter name="equipment" required="false" />
        /// </request>
        [HttpNoContentResponse]
        [Route("{id:int}"), AuthorizeAdmin(AccessKeyAction = "ManageDeviceClass")]
        public void Put(int id, JObject json)
        {
            var deviceClass = DataContext.DeviceClass.Get(id);
            if (deviceClass == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device class not found!");

            Mapper.Apply(deviceClass, json);
            Validate(deviceClass);
            deviceClass.Equipment.ForEach(e => Validate(e));

            var existing = DataContext.DeviceClass.Get(deviceClass.Name, deviceClass.Version);
            if (existing != null && existing.ID != deviceClass.ID)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Device class with such name and version already exists!");

            DataContext.DeviceClass.Save(deviceClass);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing device class.
        /// </summary>
        /// <param name="id">Device class identifier.</param>
        [HttpNoContentResponse]
        [Route("{id:int}"), AuthorizeAdmin(AccessKeyAction = "ManageDeviceClass")]
        public void Delete(int id)
        {
            var devices = DataContext.Device.GetAll(new DeviceFilter { DeviceClassID = id });
            if (devices.Any())
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Could not delete a device class because there are one or several devices associated with it.");

            DataContext.DeviceClass.Delete(id);
        }

        private IJsonMapper<DeviceClass> Mapper
        {
            get { return GetMapper<DeviceClass>(); }
        }
    }
}