using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="Equipment" />
    [AuthorizeAdmin(AccessKeyAction = "ManageDeviceClass")]
    [ApiExplorerSettings(IgnoreApi = true)] // backward compatibility only
    [RoutePrefix("device/class/{deviceClassId:int}/equipment")]
    public class EquipmentController : BaseController
    {
        [Route]
        public HttpResponseMessage Get(int deviceClassId)
        {
            return HttpResponse(HttpStatusCode.MethodNotAllowed, "The method is not allowed");
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about particular equipment of the device class.
        /// </summary>
        /// <param name="deviceClassId">Device class identifier.</param>
        /// <param name="id">Equipment identifier.</param>
        /// <returns cref="Equipment">If successful, this method returns an <see cref="Equipment"/> resource in the response body.</returns>
        [Route("{id:int}")]
        public JObject Get(int deviceClassId, int id)
        {
            var deviceClass = DataContext.DeviceClass.Get(deviceClassId);
            var equipment = deviceClass == null ? null : deviceClass.Equipment.FirstOrDefault(e => e.ID == id);
            if (equipment == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Equipment not found!");

            return Mapper.Map(equipment);
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new equipment in the device class.
        /// </summary>
        /// <param name="deviceClassId">Device class identifier.</param>
        /// <param name="json" cref="Equipment">In the request body, supply an <see cref="Equipment"/> resource.</param>
        /// <returns cref="Equipment" mode="OneWayOnly">If successful, this method returns an <see cref="Equipment"/> resource in the response body.</returns>
        [Route]
        [HttpCreatedResponse]
        public JObject Post(int deviceClassId, JObject json)
        {
            var deviceClass = DataContext.DeviceClass.Get(deviceClassId);
            if (deviceClass == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device class not found!");

            var equipment = Mapper.Map(json);
            Validate(equipment);

            deviceClass.Equipment.Add(equipment);
            DataContext.DeviceClass.Save(deviceClass);
            return Mapper.Map(equipment, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing equipment in the device class.
        /// </summary>
        /// <param name="deviceClassId">Device class identifier.</param>
        /// <param name="id">Equipment identifier.</param>
        /// <param name="json" cref="Equipment">In the request body, supply an <see cref="Equipment"/> resource.</param>
        [Route("{id:int}")]
        [HttpNoContentResponse]
        public void Put(int deviceClassId, int id, JObject json)
        {
            var deviceClass = DataContext.DeviceClass.Get(deviceClassId);
            var equipment = deviceClass == null ? null : deviceClass.Equipment.FirstOrDefault(e => e.ID == id);
            if (equipment == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Equipment not found!");

            Mapper.Apply(equipment, json);
            Validate(equipment);

            DataContext.DeviceClass.Save(deviceClass);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing equipment in the device class.
        /// </summary>
        /// <param name="deviceClassId">Device class identifier.</param>
        /// <param name="id">Equipment identifier.</param>
        [Route("{id:int}")]
        [HttpNoContentResponse]
        public void Delete(int deviceClassId, int id)
        {
            var deviceClass = DataContext.DeviceClass.Get(deviceClassId);
            var equipment = deviceClass == null ? null : deviceClass.Equipment.FirstOrDefault(e => e.ID == id);
            if (equipment != null)
            {
                deviceClass.Equipment.Remove(equipment);
                DataContext.DeviceClass.Save(deviceClass);
            }
        }

        private IJsonMapper<Equipment> Mapper
        {
            get { return GetMapper<Equipment>(); }
        }
    }
}