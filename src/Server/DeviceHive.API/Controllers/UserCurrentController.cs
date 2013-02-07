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
    [AuthorizeUser]
    public class UserCurrentController : BaseController
    {
        /// <name>getCurrent</name>
        /// <summary>
        /// Gets information about the current user and its assigned networks.
        /// </summary>
        /// <returns cref="User">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <response>
        ///     <parameter name="networks" type="array" cref="UserNetwork">Array of networks associated with the user</parameter>
        /// </response>
        public JObject Get()
        {
            var jUser = Mapper.Map(RequestContext.CurrentUser);

            var userNetworkMapper = GetMapper<UserNetwork>();
            var userNetworks = DataContext.UserNetwork.GetByUser(RequestContext.CurrentUser.ID);
            jUser["networks"] = new JArray(userNetworks.Select(un => userNetworkMapper.Map(un)));
            return jUser;
        }

        /// <name>updateCurrent</name>
        /// <summary>
        /// Updates the current user.
        /// </summary>
        /// <param name="json">In the request body, supply a <see cref="User"/> resource.</param>
        /// <returns cref="User">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="password" type="string">User password</parameter>
        /// </request>
        public JObject Put(JObject json)
        {
            var user = DataContext.User.Get(RequestContext.CurrentUser.ID);

            if (json["password"] != null && json["password"].Type == JTokenType.String)
                user.SetPassword((string)json["password"]);
            Validate(user);

            DataContext.User.Save(user);
            return Mapper.Map(user);
        }

        private IJsonMapper<User> Mapper
        {
            get { return GetMapper<User>(); }
        }
    }
}