using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using DeviceHive.API.Filters;
using DeviceHive.API.Mapping;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="Network" />
    public class NetworkController : BaseController
    {
        /// <name>list</name>
        /// <summary>
        /// Gets list of device networks.
        /// <para>If caller belongs to the Client user role, the result list is limited to networks the user has access to.</para>
        /// </summary>
        /// <returns cref="Network">If successful, this method returns array of <see cref="Network"/> resources in the response body.</returns>
        [AuthorizeUser]
        public JArray Get()
        {
            var networks = RequestContext.CurrentUser.Role == (int)UserRole.Client ?
                DataContext.Network.GetByUser(RequestContext.CurrentUser.ID) :
                DataContext.Network.GetAll();

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
        [AuthorizeUser]
        public JObject Get(int id)
        {
            var network = DataContext.Network.Get(id);
            if (network == null || !IsNetworkAccessible(network.ID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Network not found!");

            var jNetwork = Mapper.Map(network);

            var deviceMapper = GetMapper<Device>();
            var devices = DataContext.Device.GetByNetwork(id);
            jNetwork["devices"] = new JArray(devices.Select(d => deviceMapper.Map(d)));
            return jNetwork;
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new device network.
        /// </summary>
        /// <param name="json" cref="Network">In the request body, supply a <see cref="Network"/> resource.</param>
        /// <returns cref="Network">If successful, this method returns a <see cref="Network"/> resource in the response body.</returns>
        [HttpCreatedResponse]
        [AuthorizeUser(Roles = "Administrator")]
        public JObject Post(JObject json)
        {
            var network = Mapper.Map(json);
            Validate(network);
            
            if (DataContext.Network.Get(network.Name) != null)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Network with such name already exists!");
            
            DataContext.Network.Save(network);
            return Mapper.Map(network);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing device network.
        /// </summary>
        /// <param name="id">Network identifier.</param>
        /// <param name="json" cref="Network">In the request body, supply a <see cref="Network"/> resource.</param>
        /// <returns cref="Network">If successful, this method returns a <see cref="Network"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="name" required="false" />
        /// </request>
        [AuthorizeUser(Roles = "Administrator")]
        public JObject Put(int id, JObject json)
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
            return Mapper.Map(network);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing device network.
        /// </summary>
        /// <param name="id">Network identifier.</param>
        [HttpNoContentResponse]
        [AuthorizeUser(Roles = "Administrator")]
        public void Delete(int id)
        {
            DataContext.Network.Delete(id);
        }

        private IJsonMapper<Network> Mapper
        {
            get
            {
                var mapper = GetMapper<Network>();
                if (RequestContext.CurrentUser.Role == (int)UserRole.Client)
                    return new ClientNetworkMapper(mapper);
                return mapper;
            }
        }

        private class ClientNetworkMapper : IJsonMapper<Network>
        {
            private IJsonMapper<Network> _mapper;

            #region Constructor

            public ClientNetworkMapper(IJsonMapper<Network> mapper)
            {
                if (mapper == null)
                    throw new ArgumentNullException("mapper");

                _mapper = mapper;
            }
            #endregion

            #region IJsonMapper<Network> Members

            public JObject Map(Network entity)
            {
                var jObject = _mapper.Map(entity);
                jObject.Remove("key"); // do not expose network key to clients
                return jObject;
            }

            public Network Map(JObject json)
            {
                return _mapper.Map(json);
            }

            public void Apply(Network entity, JObject json)
            {
                _mapper.Apply(entity, json);
            }
            #endregion
        }
    }
}