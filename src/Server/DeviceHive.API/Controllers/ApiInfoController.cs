using System;
using System.Configuration;
using System.Web.Http;
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

        public ApiInfoController(ITimestampRepository timestampRepository)
        {
            _timestampRepository = timestampRepository;
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

        private IJsonMapper<ApiInfo> Mapper
        {
            get { return GetMapper<ApiInfo>(); }
        }
    }
}
