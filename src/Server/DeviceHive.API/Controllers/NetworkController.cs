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
    /// <resource cref="Network" />
    [RoutePrefix("network")]
    public class NetworkController : BaseController
    {
        /// <name>list</name>
        /// <summary>
        /// Gets list of device networks.
        /// <para>The result list is limited to networks the client has access to.</para>
        /// </summary>
        /// <query cref="NetworkFilter" />
        /// <returns cref="Network">If successful, this method returns array of <see cref="Network"/> resources in the response body.</returns>
        [Route, AuthorizeUser(AccessKeyAction = "GetNetwork")]
        public JArray Get()
        {
            var filter = MapObjectFromQuery<NetworkFilter>();

            var networks = CallContext.CurrentUser.Role == (int)UserRole.Administrator ?
                DataContext.Network.GetAll(filter) :
                DataContext.Network.GetByUser(CallContext.CurrentUser.ID, filter);

            if (CallContext.CurrentUserPermissions != null)
            {
                // if access key was used, limit networks to allowed ones
                networks = networks.Where(n => CallContext.CurrentUserPermissions.Any(p => p.IsNetworkAllowed(n.ID))).ToList();
            }

            return new JArray(networks.Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device network and its devices.
        /// </summary>
        /// <param name="id">Network identifier.</param>
        /// <returns cref="Network">If successful, this method returns a <see cref="Network"/> resource in the response body.</returns>
        /// <response>
        ///     <parameter name="devices" type="array" cref="Device">Array of devices registered in the current network.</parameter>
        ///     <parameter name="devices[].network" mode="remove" />
        /// </response>
        [Route("{id:int}"), AuthorizeUser(AccessKeyAction = "GetNetwork")]
        public JObject Get(int id)
        {
            var network = DataContext.Network.Get(id);
            if (network == null || !IsNetworkAccessible(network))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Network not found!");

            var jNetwork = Mapper.Map(network);

            var deviceMapper = GetMapper<Device>();
            var devices = DataContext.Device.GetByNetwork(id);
            if (CallContext.CurrentUserPermissions != null)
            {
                // if access key was used, limit devices to allowed ones
                devices = devices.Where(d => CallContext.CurrentUserPermissions.Any(p =>
                    p.IsActionAllowed("GetDevice") && p.IsDeviceAllowed(d.GUID))).ToList();
            }
            
            jNetwork["devices"] = new JArray(devices.Select(d => deviceMapper.Map(d)));
            return jNetwork;
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new device network.
        /// </summary>
        /// <param name="json" cref="Network">In the request body, supply a <see cref="Network"/> resource.</param>
        /// <returns cref="Network" mode="OneWayOnly">If successful, this method returns a <see cref="Network"/> resource in the response body.</returns>
        [HttpCreatedResponse]
        [Route, AuthorizeAdmin]
        public JObject Post(JObject json)
        {
            var network = Mapper.Map(json);
            Validate(network);
            
            if (DataContext.Network.Get(network.Name) != null)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Network with such name already exists!");
            
            DataContext.Network.Save(network);
            return Mapper.Map(network, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing device network.
        /// </summary>
        /// <param name="id">Network identifier.</param>
        /// <param name="json" cref="Network">In the request body, supply a <see cref="Network"/> resource.</param>
        /// <request>
        ///     <parameter name="name" required="false" />
        /// </request>
        [HttpNoContentResponse]
        [Route("{id:int}"), AuthorizeAdmin]
        public void Put(int id, JObject json)
        {
            var network = DataContext.Network.Get(id);
            if (network == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Network not found!");

            Mapper.Apply(network, json);
            Validate(network);

            var existing = DataContext.Network.Get(network.Name);
            if (existing != null && existing.ID != network.ID)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Network with such name already exists!");

            DataContext.Network.Save(network);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing device network.
        /// </summary>
        /// <param name="id">Network identifier.</param>
        [HttpNoContentResponse]
        [Route("{id:int}"), AuthorizeAdmin]
        public void Delete(int id)
        {
            DataContext.Network.Delete(id);
        }

        private IJsonMapper<Network> Mapper
        {
            get { return GetMapper<Network>(); }
        }
    }
}