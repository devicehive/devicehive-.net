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
    /// <resource cref="User" />
    [AuthorizeUser(Roles = "Administrator")]
    public class UserController : BaseController
    {
        /// <name>list</name>
        /// <summary>
        /// Gets list of users.
        /// </summary>
        /// <returns cref="User">If successful, this method returns array of <see cref="User"/> resources in the response body.</returns>
        public JArray Get()
        {
            return new JArray(DataContext.User.GetAll().Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about user and its assigned networks.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <returns cref="User">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <response>
        ///     <parameter name="networks" type="array" cref="UserNetwork">Array of networks associated with the user</parameter>
        /// </response>
        public JObject Get(int id)
        {
            var user = DataContext.User.Get(id);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            var jUser = Mapper.Map(user);

            var userNetworkMapper = GetMapper<UserNetwork>();
            var userNetworks = DataContext.UserNetwork.GetByUser(id);
            jUser["networks"] = new JArray(userNetworks.Select(un => userNetworkMapper.Map(un)));
            return jUser;
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new user.
        /// </summary>
        /// <param name="json" cref="User">In the request body, supply a <see cref="User"/> resource.</param>
        /// <returns cref="User">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="password" type="string" required="true">User password</parameter>
        /// </request>
        [HttpCreatedResponse]
        public JObject Post(JObject json)
        {
            if (json["password"] == null || json["password"].Type != JTokenType.String)
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Required 'password' property was not specified!");

            var user = Mapper.Map(json);
            user.SetPassword((string)json["password"]);
            Validate(user);

            if (DataContext.User.Get(user.Login) != null)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "User with such login already exists!");

            DataContext.User.Save(user);
            return Mapper.Map(user);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="json" cref="User">In the request body, supply a <see cref="User"/> resource.</param>
        /// <returns cref="Network">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="password" type="string">User password</parameter>
        ///     <parameter name="login" required="false" />
        ///     <parameter name="role" required="false" />
        ///     <parameter name="status" required="false" />
        /// </request>
        public JObject Put(int id, JObject json)
        {
            var user = DataContext.User.Get(id);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            Mapper.Apply(user, json);
            if (json["password"] != null && json["password"].Type == JTokenType.String)
                user.SetPassword((string)json["password"]);
            Validate(user);

            var existing = DataContext.User.Get(user.Login);
            if (existing != null && existing.ID != user.ID)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "User with such name already exists!");

            DataContext.User.Save(user);
            return Mapper.Map(user);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        [HttpNoContentResponse]
        public void Delete(int id)
        {
            DataContext.User.Delete(id);
        }

        private IJsonMapper<User> Mapper
        {
            get { return GetMapper<User>(); }
        }
    }
}