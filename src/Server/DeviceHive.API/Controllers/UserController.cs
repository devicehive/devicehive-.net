using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using DeviceHive.API.Filters;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="User" />
    [RoutePrefix("user")]
    public class UserController : BaseController
    {
        private readonly PasswordPolicyValidator _passwordPolicyValidator;

        public UserController(PasswordPolicyValidator passwordPolicyValidator)
        {
            _passwordPolicyValidator = passwordPolicyValidator;
        }

        /// <name>list</name>
        /// <summary>
        /// Gets list of users.
        /// </summary>
        /// <query cref="UserFilter" />
        /// <returns cref="User">If successful, this method returns array of <see cref="User"/> resources in the response body.</returns>
        [Route, AuthorizeAdmin(AccessKeyAction = "ManageUser")]
        public JArray Get()
        {
            var filter = MapObjectFromQuery<UserFilter>();
            return new JArray(DataContext.User.GetAll(filter).Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about user and its assigned networks.
        /// <para>
        /// Only administrators are allowed to get information about any user.
        /// User-level accounts can only retrieve information about themselves.
        /// </para>
        /// </summary>
        /// <param name="id">User identifier. Use the 'current' keyword to get information about the current user.</param>
        /// <returns cref="User">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <response>
        ///     <parameter name="networks" type="array" cref="UserNetwork">Array of networks associated with the user</parameter>
        /// </response>
        [Route("{id:idorcurrent}")]
        [AuthorizeAdminOrCurrentUser("id", AccessKeyAction = "ManageUser", CurrentUserAccessKeyAction = "GetCurrentUser")]
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
        /// <returns cref="User" mode="OneWayOnly">If successful, this method returns a <see cref="User"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="password" type="string" required="true">User password</parameter>
        /// </request>
        [Route, AuthorizeAdmin(AccessKeyAction = "ManageUser")]
        [HttpCreatedResponse]
        public JObject Post(JObject json)
        {
            var user = Mapper.Map(json);
            Validate(user);
            ValidateLoginUniqueness(user);

            var password = (string)json["password"];
            if (password != null)
            {
                ValidatePasswordPolicy(password);
                user.SetPassword(password);
            }

            DataContext.User.Save(user);
            return Mapper.Map(user, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing user.
        /// <para>
        /// Only administrators are allowed to update any property of any user.
        /// User-level accounts can only change their own password in case:
        /// </para>
        /// <list type="bullet">
        ///     <item>They already have a password.</item>
        ///     <item>They provide a valid current password in the 'oldPassword' property.</item>
        /// </list>
        /// </summary>
        /// <param name="id">User identifier. Use the 'current' keyword to update information of the current user.</param>
        /// <param name="json" cref="User">In the request body, supply a <see cref="User"/> resource.</param>
        /// <request>
        ///     <parameter name="password" type="string">User new password</parameter>
        ///     <parameter name="oldPassword" type="string">User current password (for non-administrative password changing functionality only)</parameter>
        ///     <parameter name="login" required="false" />
        ///     <parameter name="role" required="false" />
        ///     <parameter name="status" required="false" />
        /// </request>
        [HttpNoContentResponse]
        [Route("{id:idorcurrent}")]
        [AuthorizeAdminOrCurrentUser("id", AccessKeyAction = "ManageUser", CurrentUserAccessKeyAction = "UpdateCurrentUser")]
        public void Put(int id, JObject json)
        {
            var user = DataContext.User.Get(id);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            // only administrators can change user properties
            if (CallContext.CurrentUser.Role == (int)UserRole.Administrator)
            {
                Mapper.Apply(user, json);
                Validate(user);
                ValidateLoginUniqueness(user);
            }

            // all users can change their password
            var password = (string)json["password"];
            if (password != null)
            {
                // validate password policy
                ValidatePasswordPolicy(password);

                // additional checks for non-administrative users or password changing request
                var oldPassword = (string)json["oldPassword"];
                if (CallContext.CurrentUser.Role != (int)UserRole.Administrator || oldPassword != null)
                {
                    if (oldPassword == null)
                        ThrowHttpResponse(HttpStatusCode.Forbidden, "Please provide an old password in order to change it!");
                    if (!user.HasPassword())
                        ThrowHttpResponse(HttpStatusCode.Forbidden, "It's not allowed to change a password for an user with the social login option only!");
                    if (!user.IsValidPassword(oldPassword))
                        ThrowHttpResponse(HttpStatusCode.Forbidden, "Invalid old password supplied!");
                }

                user.SetPassword(password);
            }

            DataContext.User.Save(user);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        [HttpNoContentResponse]
        [Route("{id:int}"), AuthorizeAdmin(AccessKeyAction = "ManageUser")]
        public void Delete(int id)
        {
            DataContext.User.Delete(id);
        }

        private void ValidateLoginUniqueness(User user)
        {
            var existing = DataContext.User.Get(user.Login);
            if (existing != null && existing.ID != user.ID)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "User with such login already exists!");

            if (user.FacebookLogin != null)
            {
                existing = DataContext.User.GetByFacebookLogin(user.FacebookLogin);
                if (existing != null && existing.ID != user.ID)
                    ThrowHttpResponse(HttpStatusCode.Forbidden, "User with such Facebook login already exists!");
            }

            if (user.GoogleLogin != null)
            {
                existing = DataContext.User.GetByGoogleLogin(user.GoogleLogin);
                if (existing != null && existing.ID != user.ID)
                    ThrowHttpResponse(HttpStatusCode.Forbidden, "User with such Google login already exists!");
            }

            if (user.GithubLogin != null)
            {
                existing = DataContext.User.GetByGithubLogin(user.GithubLogin);
                if (existing != null && existing.ID != user.ID)
                    ThrowHttpResponse(HttpStatusCode.Forbidden, "User with such Github login already exists!");
            }
        }

        private void ValidatePasswordPolicy(string password)
        {
            try
            {
                _passwordPolicyValidator.Validate(password);
            }
            catch (PasswordPolicyViolationException e)
            {
                ThrowHttpResponse(HttpStatusCode.Forbidden, e.Message);
            }
        }

        private IJsonMapper<User> Mapper
        {
            get { return GetMapper<User>(); }
        }
    }
}