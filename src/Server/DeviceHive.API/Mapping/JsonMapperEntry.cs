using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Mapping
{
    /// <summary>
    /// Represents mapping entry for <see cref="JsonMapper{T}"/>
    /// </summary>
    public class JsonMapperEntry<T> : IJsonMapperEntry
    {
        private Action<T, JObject> _mapToJsonAction;
        private Action<JObject, T> _mapToEntityAction;

        #region IJsonMapperEntry Members

        /// <summary>
        /// Gets entry mapping mode
        /// </summary>
        public JsonMapperEntryMode Mode { get; private set; }

        /// <summary>
        /// Gets name of corresponding json property
        /// </summary>
        public string JsonProperty { get; private set; }

        /// <summary>
        /// Gets corresponding domain object property
        /// </summary>
        public PropertyInfo EntityProperty { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="mode">Mapping mode</param>
        /// <param name="jsonProperty">Json property name</param>
        /// <param name="entityProperty">Entity property</param>
        /// <param name="mapToJsonAction">Delegate to set entity property to json object</param>
        /// <param name="mapToEntityAction">Delagate to set json property to entity object</param>
        public JsonMapperEntry(JsonMapperEntryMode mode, string jsonProperty, PropertyInfo entityProperty,
            Action<T, JObject> mapToJsonAction, Action<JObject, T> mapToEntityAction)
        {
            Mode = mode;
            JsonProperty = jsonProperty;
            EntityProperty = entityProperty;

            _mapToJsonAction = mapToJsonAction;
            _mapToEntityAction = mapToEntityAction;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Sets entity property to json object
        /// </summary>
        /// <param name="entity">Source entity object</param>
        /// <param name="json">Target json object</param>
        public virtual void MapToJson(T entity, JObject json)
        {
            _mapToJsonAction(entity, json);
        }

        /// <summary>
        /// Sets json property to entity object
        /// </summary>
        /// <param name="json">Source json object</param>
        /// <param name="entity">Target entity object</param>
        public virtual void MapToEntity(JObject json, T entity)
        {
            _mapToEntityAction(json, entity);
        }
        #endregion
    }
}