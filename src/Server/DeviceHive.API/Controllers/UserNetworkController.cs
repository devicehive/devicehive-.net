using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="User" />
    [AuthorizeUser(Roles = "Administrator")]
    public class UserNetworkController : BaseController
    {
        /// <name>getNetwork</name>
        /// <summary>
        /// Gets information about user/network association.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="networkId">Network identifier.</param>
        /// <returns cref="UserNetwork">If successful, this method returns the following structure in the response body.</returns>
        public JObject Get(int id, int networkId)
        {
            var user = DataContext.User.Get(id);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            var network = DataContext.Network.Get(networkId);
            if (network == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Network not found!");

            var userNetwork = DataContext.UserNetwork.Get(id, networkId);
            if (userNetwork == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User/network association not found!");

            return Mapper.Map(userNetwork);
        }

        /// <name>assignNetwork</name>
        /// <summary>
        /// Associates network with the user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="networkId">Network identifier.</param>
        /// <param name="json" cref="UserNetwork">In the request body, supply the empty object.</param>
        /// <returns cref="UserNetwork">If successful, this method returns the following structure in the response body.</returns>
        /// <request>
        ///     <parameter name="network" mode="remove" />
        /// </request>
        public JObject Put(int id, int networkId, JObject json)
        {
            var user = DataContext.User.Get(id);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            var network = DataContext.Network.Get(networkId);
            if (network == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Network not found!");

            var userNetwork = DataContext.UserNetwork.Get(id, networkId);
            if (userNetwork == null)
                userNetwork = new UserNetwork(user, network);

            Mapper.Apply(userNetwork, json);
            Validate(userNetwork);

            DataContext.UserNetwork.Save(userNetwork);
            return Mapper.Map(userNetwork);
        }

        /// <name>unassignNetwork</name>
        /// <summary>
        /// Removes association between network and user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="networkId">Network identifier.</param>
        [HttpNoContentResponse]
        public void Delete(int id, int networkId)
        {
            var userNetwork = DataContext.UserNetwork.Get(id, networkId);
            if (userNetwork != null)
                DataContext.UserNetwork.Delete(userNetwork.ID);
        }

        private IJsonMapper<UserNetwork> Mapper
        {
            get { return GetMapper<UserNetwork>(); }
        }
    }
}