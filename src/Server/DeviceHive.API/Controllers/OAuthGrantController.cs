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
    /// <resource cref="OAuthGrant" />
    [AuthorizeUser, ResolveCurrentUser("userId")]
    public class OAuthGrantController : BaseController
    {
        /// <name>list</name>
        /// <summary>
        /// Gets list of OAuth grants.
        /// </summary>
        /// <query cref="DeviceFilter" />
        /// <param name="userId">User identifier. Use the 'current' keyword to list access keys of the current user.</param>
        /// <returns cref="OAuthGrant">If successful, this method returns array of <see cref="OAuthGrant"/> resources in the response body.</returns>
        public JArray Get(int userId)
        {
            EnsureUserAccessTo(userId);

            var user = DataContext.User.Get(userId);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            var filter = MapObjectFromQuery<OAuthGrantFilter>();
            return new JArray(DataContext.OAuthGrant.GetByUser(user.ID, filter).Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about OAuth grant.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to get access key of the current user.</param>
        /// <param name="id">OAuth grant identifier.</param>
        /// <returns cref="OAuthGrant">If successful, this method returns a <see cref="OAuthGrant"/> resource in the response body.</returns>
        public JObject Get(int userId, int id)
        {
            EnsureUserAccessTo(userId);

            var oauthGrant = DataContext.OAuthGrant.Get(id);
            if (oauthGrant == null || oauthGrant.UserID != userId)
                ThrowHttpResponse(HttpStatusCode.NotFound, "OAuth grant not found!");

            return Mapper.Map(oauthGrant);
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new OAuth grant.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to create access key for the current user.</param>
        /// <param name="json" cref="OAuthGrant">In the request body, supply a <see cref="OAuthGrant"/> resource.</param>
        /// <returns cref="OAuthGrant" mode="OneWayOnly">If successful, this method returns a <see cref="OAuthGrant"/> resource in the response body.</returns>
        [HttpCreatedResponse]
        public JObject Post(int userId, JObject json)
        {
            EnsureUserAccessTo(userId);

            var user = DataContext.User.Get(userId);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            var oauthGrant = Mapper.Map(json);
            oauthGrant.UserID = user.ID;
            if (oauthGrant.Type == (int)OAuthGrantType.Code)
                oauthGrant.AuthCode = Guid.NewGuid();

            MapClient(oauthGrant);
            CreateAccessKey(user, oauthGrant);
            Validate(oauthGrant);

            DataContext.AccessKey.Save(oauthGrant.AccessKey);
            DataContext.OAuthGrant.Save(oauthGrant);

            if (oauthGrant.Type == (int)OAuthGrantType.Code)
                oauthGrant.AccessKey = null; // do not disclose key in authorization code scenario

            return Mapper.Map(oauthGrant, oneWayOnly: true);
        }

        /// <name>update</name>
        /// <summary>
        /// Updates an existing OAuth grant.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to update access key of the current user.</param>
        /// <param name="id">OAuth grant identifier.</param>
        /// <param name="json" cref="OAuthGrant">In the request body, supply a <see cref="OAuthGrant"/> resource.</param>
        /// <request>
        ///     <parameter name="type" required="false" />
        ///     <parameter name="scope" required="false" />
        ///     <parameter name="redirectUrl" required="false" />
        ///     <parameter name="accessType" required="false" />
        /// </request>
        /// <returns cref="OAuthGrant" mode="OneWayOnly">If successful, this method returns a <see cref="OAuthGrant"/> resource in the response body.</returns>
        public JObject Put(int userId, int id, JObject json)
        {
            EnsureUserAccessTo(userId);

            var oauthGrant = DataContext.OAuthGrant.Get(id);
            if (oauthGrant == null || oauthGrant.UserID != userId)
                ThrowHttpResponse(HttpStatusCode.NotFound, "OAuth grant not found!");

            Mapper.Apply(oauthGrant, json);
            oauthGrant.AuthCode = oauthGrant.Type == (int)OAuthGrantType.Code ? (Guid?)Guid.NewGuid() : null;

            MapClient(oauthGrant);
            UpdateAccessKey(oauthGrant);
            Validate(oauthGrant);

            DataContext.AccessKey.Save(oauthGrant.AccessKey);
            DataContext.OAuthGrant.Save(oauthGrant);

            if (oauthGrant.Type == (int)OAuthGrantType.Code)
                oauthGrant.AccessKey = null; // do not disclose key in authorization code scenario

            return Mapper.Map(oauthGrant, oneWayOnly: true);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing OAuth grant.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to delete access key of the current user.</param>
        /// <param name="id">OAuth grant identifier.</param>
        [HttpNoContentResponse]
        public void Delete(int userId, int id)
        {
            EnsureUserAccessTo(userId);

            var oauthGrant = DataContext.OAuthGrant.Get(id);
            if (oauthGrant != null && oauthGrant.UserID == userId)
            {
                DataContext.OAuthGrant.Delete(id);
                DataContext.AccessKey.Delete(oauthGrant.AccessKeyID);
            }
        }

        private void MapClient(OAuthGrant grant)
        {
            if (grant.Client == null)
                ThrowHttpResponse(HttpStatusCode.BadRequest, "The 'client' field is required!");
            if (grant.Client.OAuthID == null)
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Specified 'client' object must include 'oauthId' property!");

            var client = DataContext.OAuthClient.Get(grant.Client.OAuthID);
            if (client == null)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "A client with specified 'oauthId' property does not exist!");

            grant.Client = client;
        }

        private void CreateAccessKey(User user, OAuthGrant grant)
        {
            var accessKey = new AccessKey(user.ID, string.Format("Key for {0} client", grant.Client.Name));
            if (grant.AccessType == (int)OAuthGrantAccessType.Online)
                accessKey.ExpirationDate = DataContext.Timestamp.GetCurrentTimestamp().AddHours(1);

            accessKey.Permissions.Add(new AccessKeyPermission
                {
                    Subnets = grant.Client.Subnet == null ? null : grant.Client.Subnet.Split(','),
                    Domains = new[] { grant.Client.Domain },
                    Actions = grant.Scope.Split(' '),
                });

            grant.AccessKey = accessKey;
        }

        private void UpdateAccessKey(OAuthGrant grant)
        {
            grant.AccessKey.Label = string.Format("Key for {0} client", grant.Client.Name);
            grant.AccessKey.ExpirationDate = grant.AccessType == (int)OAuthGrantAccessType.Online ?
                (DateTime?)DataContext.Timestamp.GetCurrentTimestamp().AddHours(1) : null;

            grant.AccessKey.Permissions.Clear();
            grant.AccessKey.Permissions.Add(new AccessKeyPermission
                {
                    Subnets = grant.Client.Subnet == null ? null : grant.Client.Subnet.Split(','),
                    Domains = new[] { grant.Client.Domain },
                    Actions = grant.Scope.Split(' '),
                });
        }

        private IJsonMapper<OAuthGrant> Mapper
        {
            get { return GetMapper<OAuthGrant>(); }
        }
    }
}