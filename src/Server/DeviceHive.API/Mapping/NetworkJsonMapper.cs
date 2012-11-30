using System;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.API.Mapping
{
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

            var context = _kernel.Get<RequestContext>();
            if (context.CurrentUser == null || context.CurrentUser.Role != (int)UserRole.Administrator)
            {
                json.Remove("key"); // do not expose network key to clients
            }
        }
        #endregion
    }
}