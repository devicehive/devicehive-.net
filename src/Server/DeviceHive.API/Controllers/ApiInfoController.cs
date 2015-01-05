using System;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using DeviceHive.Core.Authentication;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using Newtonsoft.Json.Linq;
using Version = DeviceHive.Core.Version;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="ApiInfo" />
    [RoutePrefix("info")]
    public class ApiInfoController : BaseController
    {
        private ITimestampRepository _timestampRepository;
        private IAuthenticationManager _authenticationManager;

        public ApiInfoController(ITimestampRepository timestampRepository, IAuthenticationManager authenticationManager)
        {
            _timestampRepository = timestampRepository;
            _authenticationManager = authenticationManager;
        }

        /// <name>get</name>
        /// <summary>
        /// Gets meta-information of the current API.
        /// </summary>
        /// <returns cref="ApiInfo">If successful, this method returns a <see cref="ApiInfo"/> resource in the response body.</returns>
        [Route]
        public JObject Get()
        {
            var webSocketEndpoint = DeviceHiveConfiguration.WebSocketEndpoint;
            var apiInfo = new ApiInfo
            {
                ApiVersion = Version.ApiVersion,
                ServerTimestamp = _timestampRepository.GetCurrentTimestamp(),
                WebSocketServerUrl = webSocketEndpoint.Enabled ? webSocketEndpoint.Url : null,
            };

            return Mapper.Map(apiInfo);
        }

        [HttpGet]
        [Route("config/oauth2")]
        public JObject OAuth2()
        {
            var index = 0;
            return new JObject(
                _authenticationManager.GetProviders().Select(p => new JProperty(p.Name,
                    new JObject(
                        new JProperty("clientId", p.Configuration.ClientId),
                        new JProperty("providerId", index++),
                        new JProperty("isAvailable", true)
                    )
                ))
            );
        }

        private IJsonMapper<ApiInfo> Mapper
        {
            get { return GetMapper<ApiInfo>(); }
        }
    }
}
