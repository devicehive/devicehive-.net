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
    /// <resource cref="OAuthGrant" />
    [RoutePrefix("user/{userId:idorcurrent}/oauth/grant")]
    [AuthorizeAdminOrCurrentUser("userId", AccessKeyAction = "ManageUser", CurrentUserAccessKeyAction = "ManageOAuthGrant")]
    public class OAuthGrantController : BaseController
    {
        /// <name>list</name>
        /// <summary>
        /// Gets list of OAuth grants.
        /// </summary>
        /// <query cref="OAuthGrantFilter" />
        /// <param name="userId">User identifier. Use the 'current' keyword to list OAuth grants of the current user.</param>
        /// <returns cref="OAuthGrant">If successful, this method returns array of <see cref="OAuthGrant"/> resources in the response body.</returns>
        [Route]
        public JArray Get(int userId)
        {
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
        /// <param name="userId">User identifier. Use the 'current' keyword to get OAuth grant of the current user.</param>
        /// <param name="id">OAuth grant identifier.</param>
        /// <returns cref="OAuthGrant">If successful, this method returns a <see cref="OAuthGrant"/> resource in the response body.</returns>
        [Route("{id:int}")]
        public JObject Get(int userId, int id)
        {
            var oauthGrant = DataContext.OAuthGrant.Get(id);
            if (oauthGrant == null || oauthGrant.UserID != userId)
                ThrowHttpResponse(HttpStatusCode.NotFound, "OAuth grant not found!");

            return Mapper.Map(oauthGrant);
        }

        /// <name>insert</name>
        /// <summary>
        /// Creates new OAuth grant.
        /// </summary>
        /// <param name="userId">User identifier. Use the 'current' keyword to create OAuth grant for the current user.</param>
        /// <param name="json" cref="OAuthGrant">In the request body, supply a <see cref="OAuthGrant"/> resource.</param>
        /// <request>
        ///     <parameter name="accessType" required="false" />
        ///     <parameter name="redirectUri" required="true" />
        ///     <parameter name="client" required="true">A <see cref="OAuthClient"/> object which includes oauthId property to match.</parameter>
        ///     <parameter name="client." mode="remove" />
        ///     <parameter name="client.oauthId" type="string" required="true" after="client">Client OAuth identifier.</parameter>
        /// </request>
        /// <returns cref="OAuthGrant" mode="OneWayOnly">If successful, this method returns a <see cref="OAuthGrant"/> resource in the response body.</returns>
        [Route]
        [HttpCreatedResponse]
        public JObject Post(int userId, JObject json)
        {
            var user = DataContext.User.Get(userId);
            if (user == null)
                ThrowHttpResponse(HttpStatusCode.NotFound, "User not found!");

            var oauthGrant = Mapper.Map(json);
            oauthGrant.UserID = user.ID;
            if (string.IsNullOrEmpty(oauthGrant.RedirectUri))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Missing required field: redirectUri");

            MapClient(oauthGrant);
            OAuth2Controller.RenewGrant(oauthGrant);
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
        /// <param name="userId">User identifier. Use the 'current' keyword to update OAuth grant of the current user.</param>
        /// <param name="id">OAuth grant identifier.</param>
        /// <param name="json" cref="OAuthGrant">In the request body, supply a <see cref="OAuthGrant"/> resource.</param>
        /// <request>
        ///     <parameter name="client" required="false">A <see cref="OAuthClient"/> object which includes oauthId property to match.</parameter>
        ///     <parameter name="client." mode="remove" />
        ///     <parameter name="client.oauthId" type="string" required="true" after="client">Client OAuth identifier.</parameter>
        ///     <parameter name="type" required="false" />
        ///     <parameter name="scope" required="false" />
        ///     <parameter name="redirectUri" required="false" />
        ///     <parameter name="accessType" required="false" />
        /// </request>
        /// <returns cref="OAuthGrant" mode="OneWayOnly">If successful, this method returns a <see cref="OAuthGrant"/> resource in the response body.</returns>
        [Route("{id:int}")]
        public JObject Put(int userId, int id, JObject json)
        {
            var oauthGrant = DataContext.OAuthGrant.Get(id);
            if (oauthGrant == null || oauthGrant.UserID != userId)
                ThrowHttpResponse(HttpStatusCode.NotFound, "OAuth grant not found!");

            Mapper.Apply(oauthGrant, json);
            if (string.IsNullOrEmpty(oauthGrant.RedirectUri))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Missing required field: redirectUri");

            MapClient(oauthGrant);
            OAuth2Controller.RenewGrant(oauthGrant);
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
        /// <param name="userId">User identifier. Use the 'current' keyword to delete OAuth grant of the current user.</param>
        /// <param name="id">OAuth grant identifier.</param>
        [Route("{id:int}")]
        [HttpNoContentResponse]
        public void Delete(int userId, int id)
        {
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

        private IJsonMapper<OAuthGrant> Mapper
        {
            get { return GetMapper<OAuthGrant>(); }
        }
    }
}