﻿using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using Ninject;
using Ninject.Activation;

namespace DeviceHive.API
{
    public class JsonMapperConfig
    {
        public static void ConfigureMapping(IContext context, JsonMapperManager manager)
        {
            context.Kernel.ConfigureMapping<User>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Login, "login")
                .Property(e => e.FacebookLogin, "facebookLogin")
                .Property(e => e.GoogleLogin, "googleLogin")
                .Property(e => e.GithubLogin, "githubLogin")
                .Property(e => e.Role, "role")
                .Property(e => e.Status, "status")
                .Property(e => e.LastLogin, "lastLogin", JsonMapperEntryMode.ToJson)
                .RawJsonProperty(e => e.Data, "data");

            context.Kernel.ConfigureMapping<Network, NetworkJsonMapper>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Key, "key") // is returned to users only
                .Property(e => e.Name, "name")
                .Property(e => e.Description, "description");

            context.Kernel.ConfigureMapping<UserNetwork>()
                .ReferenceProperty(e => e.Network, "network");

            context.Kernel.ConfigureMapping<AccessKeyPermission>()
                .Property(e => e.Domains, "domains")
                .Property(e => e.Subnets, "subnets")
                .Property(e => e.Actions, "actions")
                .Property(e => e.Networks, "networkIds")
                .Property(e => e.Devices, "deviceGuids");

            context.Kernel.ConfigureMapping<AccessKey>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Type, "type")
                .Property(e => e.Label, "label")
                .Property(e => e.Key, "key", JsonMapperEntryMode.ToJson)
                .Property(e => e.ExpirationDate, "expirationDate")
                .CollectionProperty(e => e.Permissions, "permissions");

            context.Kernel.ConfigureMapping<Equipment>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Name, "name")
                .Property(e => e.Code, "code")
                .Property(e => e.Type, "type")
                .RawJsonProperty(e => e.Data, "data");

            context.Kernel.ConfigureMapping<DeviceClass>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Name, "name")
                .Property(e => e.Version, "version")
                .Property(e => e.IsPermanent, "isPermanent")
                .Property(e => e.OfflineTimeout, "offlineTimeout")
                .RawJsonProperty(e => e.Data, "data")
                .CollectionProperty(e => e.Equipment, "equipment");

            context.Kernel.ConfigureMapping<Device>()
                .Property(e => e.GUID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Key, "key", JsonMapperEntryMode.FromJson)
                .Property(e => e.IsBlocked, "isBlocked")
                .Property(e => e.Name, "name")
                .Property(e => e.Status, "status")
                .RawJsonProperty(e => e.Data, "data")
                .ReferenceProperty(e => e.Network, "network")
                .ReferenceProperty(e => e.DeviceClass, "deviceClass");

            context.Kernel.ConfigureMapping<DeviceNotification>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.ToJson)
                .Property(e => e.Notification, "notification")
                .RawJsonProperty(e => e.Parameters, "parameters");

            context.Kernel.ConfigureMapping<DeviceCommand>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.ToJson)
                .Property(e => e.UserID, "userId", JsonMapperEntryMode.ToJson)
                .Property(e => e.Command, "command")
                .RawJsonProperty(e => e.Parameters, "parameters")
                .Property(e => e.Lifetime, "lifetime")
                .Property(e => e.Status, "status")
                .RawJsonProperty(e => e.Result, "result");

            context.Kernel.ConfigureMapping<DeviceEquipment>()
                .Property(e => e.Code, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.ToJson)
                .RawJsonProperty(e => e.Parameters, "parameters", JsonMapperEntryMode.ToJson);

            context.Kernel.ConfigureMapping<OAuthClient, OAuthClientJsonMapper>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Name, "name")
                .Property(e => e.Domain, "domain")
                .Property(e => e.Subnet, "subnet")
                .Property(e => e.RedirectUri, "redirectUri")
                .Property(e => e.OAuthID, "oauthId")
                .Property(e => e.OAuthSecret, "oauthSecret", JsonMapperEntryMode.ToJson);

            context.Kernel.ConfigureMapping<OAuthGrant>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.ToJson)
                .Property(e => e.AuthCode, "authCode", JsonMapperEntryMode.ToJson)
                .ReferenceProperty(e => e.Client, "client")
                .ReferenceProperty(e => e.AccessKey, "accessKey", JsonMapperEntryMode.ToJson)
                .EnumProperty<OAuthGrantType>(e => e.Type, "type")
                .EnumProperty<OAuthGrantAccessType>(e => e.AccessType, "accessType")
                .Property(e => e.RedirectUri, "redirectUri")
                .Property(e => e.Scope, "scope")
                .Property(e => e.Networks, "networkIds");

            context.Kernel.ConfigureMapping<ApiInfo>()
                .Property(e => e.ApiVersion, "apiVersion")
                .Property(e => e.ServerTimestamp, "serverTimestamp")
                .Property(e => e.WebSocketServerUrl, "webSocketServerUrl");

            // filters
            context.Kernel.ConfigureMapping<UserFilter>()
                .Property(e => e.Login, "login")
                .Property(e => e.LoginPattern, "loginPattern")
                .Property(e => e.Role, "role")
                .Property(e => e.Status, "status")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");

            context.Kernel.ConfigureMapping<AccessKeyFilter>()
                .Property(e => e.Label, "label")
                .Property(e => e.LabelPattern, "labelPattern")
                .Property(e => e.Type, "type")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");

            context.Kernel.ConfigureMapping<NetworkFilter>()
                .Property(e => e.Name, "name")
                .Property(e => e.NamePattern, "namePattern")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");

            context.Kernel.ConfigureMapping<DeviceClassFilter>()
                .Property(e => e.Name, "name")
                .Property(e => e.NamePattern, "namePattern")
                .Property(e => e.Version, "version")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");

            context.Kernel.ConfigureMapping<DeviceFilter>()
                .Property(e => e.Name, "name")
                .Property(e => e.NamePattern, "namePattern")
                .Property(e => e.Status, "status")
                .Property(e => e.NetworkID, "networkId")
                .Property(e => e.NetworkName, "networkName")
                .Property(e => e.DeviceClassID, "deviceClassId")
                .Property(e => e.DeviceClassName, "deviceClassName")
                .Property(e => e.DeviceClassVersion, "deviceClassVersion")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");

            context.Kernel.ConfigureMapping<DeviceNotificationFilter>()
                .Property(e => e.Start, "start")
                .Property(e => e.End, "end")
                .Property(e => e.Notification, "notification")
                .Property(e => e.GridInterval, "gridInterval")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");

            context.Kernel.ConfigureMapping<DeviceCommandFilter>()
                .Property(e => e.Start, "start")
                .Property(e => e.End, "end")
                .Property(e => e.Command, "command")
                .Property(e => e.Status, "status")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");

            context.Kernel.ConfigureMapping<OAuthClientFilter>()
                .Property(e => e.Name, "name")
                .Property(e => e.NamePattern, "namePattern")
                .Property(e => e.Domain, "domain")
                .Property(e => e.OAuthID, "oauthId")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");

            context.Kernel.ConfigureMapping<OAuthGrantFilter>()
                .Property(e => e.Start, "start")
                .Property(e => e.End, "end")
                .Property(e => e.ClientOAuthID, "clientOAuthId")
                .EnumProperty<OAuthGrantType>(e => e.Type, "type")
                .Property(e => e.Scope, "scope")
                .Property(e => e.RedirectUri, "redirectUri")
                .EnumProperty<OAuthGrantAccessType>(e => e.AccessType, "accessType")
                .Property(e => e.SortField, "sortField")
                .Property(e => e.SortOrder, "sortOrder")
                .Property(e => e.Take, "take")
                .Property(e => e.Skip, "skip");
        }
    }

    /// <summary>
    /// Represents custom mapper for Network entities
    /// </summary>
    public class NetworkJsonMapper : JsonMapper<Network>
    {
        private readonly IKernel _kernel;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="kernel">Ninject kernel</param>
        /// <param name="configuration">Mapper configuration object</param>
        public NetworkJsonMapper(IKernel kernel, JsonMapperConfiguration<Network> configuration)
            : base(configuration)
        {
            if (kernel == null)
                throw new ArgumentNullException("kernel");

            _kernel = kernel;
        }
        #endregion

        #region JsonMapper<Network> Members

        /// <summary>
        /// Executed after entity is mapped to json object.
        /// Removes the network key if current user is not an administrator
        /// </summary>
        /// <param name="entity">Source entity object</param>
        /// <param name="json">Mapped json object</param>
        protected override void OnAfterMapToJson(Network entity, JObject json)
        {
            base.OnAfterMapToJson(entity, json);

            var context = _kernel.Get<CallContext>();
            if (context.CurrentUser == null)
            {
                json.Remove("key"); // do not expose network key to devices
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents custom mapper for OAuthClient entities
    /// </summary>
    public class OAuthClientJsonMapper : JsonMapper<OAuthClient>
    {
        private readonly IKernel _kernel;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="kernel">Ninject kernel</param>
        /// <param name="configuration">Mapper configuration object</param>
        public OAuthClientJsonMapper(IKernel kernel, JsonMapperConfiguration<OAuthClient> configuration)
            : base(configuration)
        {
            if (kernel == null)
                throw new ArgumentNullException("kernel");

            _kernel = kernel;
        }
        #endregion

        #region JsonMapper<OAuthClient> Members

        /// <summary>
        /// Executed after entity is mapped to json object.
        /// Removes the oauthSecret key if current user is not an administrator
        /// </summary>
        /// <param name="entity">Source entity object</param>
        /// <param name="json">Mapped json object</param>
        protected override void OnAfterMapToJson(OAuthClient entity, JObject json)
        {
            base.OnAfterMapToJson(entity, json);

            var context = _kernel.Get<CallContext>();
            if (context.CurrentUser == null || context.CurrentUser.Role != (int)UserRole.Administrator)
            {
                json.Remove("oauthSecret"); // do not expose network key to devices
            }
        }
        #endregion
    }
}