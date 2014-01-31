using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="Device" />
    [RoutePrefix("device/{id}/equipment")]
    public class DeviceEquipmentController : BaseController
    {
        /// <name>equipment</name>
        /// <summary>
        ///     <para>Gets current state of device equipment.</para>
        ///     <para>The equipment state is tracked by framework and it could be updated by sending 'equipment' notification with the following parameters:
        ///         <list type="bullet">
        ///             <item><description>equipment: equipment code</description></item>
        ///             <item><description>parameters: current equipment state</description></item>
        ///         </list>
        ///     </para>
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        /// <returns cref="DeviceEquipment">If successful, this method returns array of the following structures in the response body.</returns>
        [Route, AuthorizeUser(AccessKeyAction = "GetDeviceState")]
        public JArray Get(Guid id)
        {
            var device = DataContext.Device.Get(id);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            return new JArray(DataContext.DeviceEquipment.GetByDevice(device.ID).Select(n => Mapper.Map(n)));
        }

        [Route("{code}"), AuthorizeUser(AccessKeyAction = "GetDeviceState")]
        public JObject Get(Guid id, string code)
        {
            var device = DataContext.Device.Get(id);
            if (device == null || !IsDeviceAccessible(device))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            var equipment = DataContext.DeviceEquipment.GetByDeviceAndCode(device.ID, code);
            if (equipment == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device equipment not found!");

            return Mapper.Map(equipment);
        }

        [Route]
        public HttpResponseMessage Post()
        {
            return HttpResponse(HttpStatusCode.MethodNotAllowed, "The method is not allowed");
        }

        private IJsonMapper<DeviceEquipment> Mapper
        {
            get { return GetMapper<DeviceEquipment>(); }
        }
    }
}