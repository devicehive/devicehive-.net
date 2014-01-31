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
    /// <resource cref="User" />
    [RoutePrefix("user")]
    public class UserController : BaseController
    {
        /// <name>list</name>
        /// <summary>
        /// Gets list of users.
        /// </summary>
        /// <query cref="UserFilter" />
        /// <returns cref="User">If successful, this method returns array of <see cref="User"/> resources in the response body.</returns>
        [Route, AuthorizeAdmin]
        public JArray Get()
        {
            var filter = MapObjectFromQuery<UserFilter>();
            return new JArray(DataContext.User.GetAll(filter).Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about user and its assigned networks.
        /// </summary>
        /// <param name="id">User identifier. Use the 'current' keyword to get information about the current user.</param>
        /// <returns cref="User">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <response>
        ///     <parameter name="networks" type="array" cref="UserNetwork">Array of networks associated with the user</parameter>
        /// </response>
        [Route("{id:idorcurrent}")]
        [AuthorizeUser, ResolveCurrentUser("id")]
        public JObject Get(int id)
        {
            EnsureUserAccessTo(id);

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
        /// <returns cref="User" mode="OneWayOnly">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="password" type="string" required="true">User password</parameter>
        /// </request>
        [Route, AuthorizeAdmin]
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
            return Mapper.Map(user, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="id">User identifier. Use the 'current' keyword to update information of the current user.</param>
        /// <param name="json" cref="User">In the request body, supply a <see cref="User"/> resource.</param>
        /// <request>
        ///     <parameter name="password" type="string">User password</parameter>
        ///     <parameter name="login" required="false" />
        ///     <parameter name="role" required="false" />
        ///     <parameter name="status" required="false" />
        /// </request>
        [HttpNoContentResponse]
        [Route("{id:idorcurrent}")]
        [AuthorizeUser, ResolveCurrentUser("id")]
        public void Put(int id, JObject json)
        {
            EnsureUserAccessTo(id);

            var user = DataContext.User.Get(id);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            if (RequestContext.CurrentUser.Role == (int)UserRole.Administrator)
            {
                // only administrators can change user properties
                Mapper.Apply(user, json);
            }
            if (json["password"] != null && json["password"].Type == JTokenType.String)
            {
                // all users can change their password
                user.SetPassword((string)json["password"]);
            }
            Validate(user);

            var existing = DataContext.User.Get(user.Login);
            if (existing != null && existing.ID != user.ID)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "User with such name already exists!");

            DataContext.User.Save(user);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        [HttpNoContentResponse]
        [Route("{id:int}"), AuthorizeAdmin]
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