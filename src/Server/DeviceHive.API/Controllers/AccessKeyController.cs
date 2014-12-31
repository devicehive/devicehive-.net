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
    /// <resource cref="AccessKey" />
    [RoutePrefix("user/{userId:idorcurrent}/accesskey")]
    [AuthorizeAdminOrCurrentUser("userId", AccessKeyAction = "ManageUser", CurrentUserAccessKeyAction = "ManageAccessKey")]
    public class AccessKeyController : BaseController
    {
        /// <name>list</name>
        /// <summary>
        /// Gets list of access keys and their permissions.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to list access keys of the current user.</param>
        /// <returns cref="AccessKey">If successful, this method returns array of <see cref="AccessKey"/> resources in the response body.</returns>
        [Route]
        public JArray Get(int userId)
        {
            var user = DataContext.User.Get(userId);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            return new JArray(DataContext.AccessKey.GetByUser(user.ID).Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about access key and its permissions.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to get access key of the current user.</param>
        /// <param name="id">Access key identifier.</param>
        /// <returns cref="AccessKey">If successful, this method returns a <see cref="AccessKey"/> resource in the response body.</returns>
        [Route("{id:int}")]
        public JObject Get(int userId, int id)
        {
            var accessKey = DataContext.AccessKey.Get(id);
            if (accessKey == null || accessKey.UserID != userId)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Access key not found!");

            return Mapper.Map(accessKey);
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new access key.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to create access key for the current user.</param>
        /// <param name="json" cref="AccessKey">In the request body, supply a <see cref="AccessKey"/> resource.</param>
        /// <returns cref="AccessKey" mode="OneWayOnly">If successful, this method returns a <see cref="AccessKey"/> resource in the response body.</returns>
        [Route]
        [HttpCreatedResponse]
        public JObject Post(int userId, JObject json)
        {
            var user = DataContext.User.Get(userId);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            var accessKey = Mapper.Map(json);
            accessKey.UserID = user.ID;
            accessKey.GenerateKey();
            Validate(accessKey);

            DataContext.AccessKey.Save(accessKey);
            
            return Mapper.Map(accessKey, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing access key.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to update access key of the current user.</param>
        /// <param name="id">Access key identifier.</param>
        /// <param name="json" cref="AccessKey">In the request body, supply a <see cref="AccessKey"/> resource.</param>
        /// <request>
        ///     <parameter name="label" required="false" />
        ///     <parameter name="expirationDate" required="false" />
        ///     <parameter name="permissions" required="false" />
        /// </request>
        [Route("{id:int}")]
        [HttpNoContentResponse]
        public void Put(int userId, int id, JObject json)
        {
            var accessKey = DataContext.AccessKey.Get(id);
            if (accessKey == null || accessKey.UserID != userId)
                ThrowHttpResponse(HttpStatusCode.NotFound, "Access key not found!");

            Mapper.Apply(accessKey, json);
            Validate(accessKey);

            DataContext.AccessKey.Save(accessKey);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing access key.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to delete access key of the current user.</param>
        /// <param name="id">Access key identifier.</param>
        [Route("{id:int}")]
        [HttpNoContentResponse]
        public void Delete(int userId, int id)
        {
            var accessKey = DataContext.AccessKey.Get(id);
            if (accessKey != null && accessKey.UserID == userId)
                DataContext.AccessKey.Delete(id);
        }

        private IJsonMapper<AccessKey> Mapper
        {
            get { return GetMapper<AccessKey>(); }
        }
    }
}