using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="Equipment" />
    [AuthorizeAdmin]
    public class EquipmentController : BaseController
    {
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
        public JObject Get(int deviceClassId, int id)
        {
            var equipment = DataContext.Equipment.Get(id);
            if (equipment == null || equipment.DeviceClassID != deviceClassId)
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
        [HttpCreatedResponse]
        public JObject Post(int deviceClassId, JObject json)
        {
            var deviceClass = DataContext.DeviceClass.Get(deviceClassId);
            if (deviceClass == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device class not found!");

            var equipment = Mapper.Map(json);
            equipment.DeviceClass = deviceClass;
            Validate(equipment);

            DataContext.Equipment.Save(equipment);
            return Mapper.Map(equipment, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing equipment in the device class.
        /// </summary>
        /// <param name="deviceClassId">Device class identifier.</param>
        /// <param name="id">Equipment identifier.</param>
        /// <param name="json" cref="Equipment">In the request body, supply an <see cref="Equipment"/> resource.</param>
        /// <request>
        ///     <parameter name="name" required="false" />
        ///     <parameter name="code" required="false" />
        ///     <parameter name="type" required="false" />
        /// </request>
        [HttpNoContentResponse]
        public void Put(int deviceClassId, int id, JObject json)
        {
            var equipment = DataContext.Equipment.Get(id);
            if (equipment == null || equipment.DeviceClassID != deviceClassId)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Equipment not found!");

            Mapper.Apply(equipment, json);
            Validate(equipment);

            DataContext.Equipment.Save(equipment);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing equipment in the device class.
        /// </summary>
        /// <param name="deviceClassId">Device class identifier.</param>
        /// <param name="id">Equipment identifier.</param>
        [HttpNoContentResponse]
        public void Delete(int deviceClassId, int id)
        {
            var equipment = DataContext.Equipment.Get(id);
            if (equipment != null && equipment.DeviceClassID == deviceClassId)
                DataContext.Equipment.Delete(id);
        }

        private IJsonMapper<Equipment> Mapper
        {
            get { return GetMapper<Equipment>(); }
        }
    }
}