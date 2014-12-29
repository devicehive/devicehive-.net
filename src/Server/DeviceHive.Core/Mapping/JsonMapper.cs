using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Core.Mapping
{
    /// <summary>
    /// Represents object to json mapper
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    public class JsonMapper<T> : IJsonMapper, IJsonMapper<T>
    {
        private readonly JsonMapperConfiguration<T> _configuration;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configuration">Mapping configuration</param>
        public JsonMapper(JsonMapperConfiguration<T> configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            _configuration = configuration;
        }
        #endregion

        #region IJsonMapper Members

        /// <summary>
        /// Gets mapper entity type
        /// </summary>
        Type IJsonMapper.EntityType
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// Gets list of mapping entries
        /// </summary>
        IList<IJsonMapperEntry> IJsonMapper.Entries
        {
            get { return _configuration.Entries.Cast<IJsonMapperEntry>().ToList(); }
        }

        /// <summary>
        /// Maps entity to json
        /// </summary>
        /// <param name="entity">Entity object</param>
        /// <returns>Mapped JObject object</returns>
        JObject IJsonMapper.Map(object entity)
        {
            if (!typeof(T).IsInstanceOfType(entity))
                throw new ArgumentException(string.Format("Entity type is invalid! Expected: {0}, Actual: {1}", typeof(T), entity.GetType()), "entity");

            return Map((T)entity);
        }

        /// <summary>
        /// Maps json to entity
        /// </summary>
        /// <param name="json">JObject object</param>
        /// <returns>Mapped entity object</returns>
        object IJsonMapper.Map(JObject json)
        {
            return Map(json);
        }

        /// <summary>
        /// Applies json fields to entity
        /// </summary>
        /// <param name="entity">Destination entity object</param>
        /// <param name="json">JObject object to apply</param>
        void IJsonMapper.Apply(object entity, JObject json)
        {
            if (!typeof(T).IsInstanceOfType(entity))
                throw new ArgumentException(string.Format("Entity type is invalid! Expected: {0}, Actual: {1}", typeof(T), entity.GetType()), "entity");

            Apply((T)entity, json);
        }
        #endregion

        #region IJsonMapper<T> Members

        /// <summary>
        /// Maps entity to json
        /// </summary>
        /// <param name="entity">Entity object</param>
        /// <param name="oneWayOnly">Whether to map only OneWay fields</param>
        /// <returns>Mapped JObject object</returns>
        public JObject Map(T entity, bool oneWayOnly = false)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var json = new JObject();
            foreach (var entry in _configuration.Entries)
            {
                if ((entry.Mode & JsonMapperEntryMode.ToJson) != 0)
                {
                    if (oneWayOnly && (entry.Mode & JsonMapperEntryMode.OneWayOnly) == 0)
                        continue;

                    entry.MapToJson(entity, json);
                }
            }

            OnAfterMapToJson(entity, json);
            return json;
        }

        /// <summary>
        /// Maps json to entity
        /// </summary>
        /// <param name="json">JObject object</param>
        /// <returns>Mapped entity object</returns>
        public T Map(JObject json)
        {
            var entity = (T)Activator.CreateInstance(typeof(T));
            Apply(entity, json);
            return entity;
        }

        /// <summary>
        /// Applies json fields to entity
        /// </summary>
        /// <param name="entity">Destination entity object</param>
        /// <param name="json">JObject object to apply</param>
        public void Apply(T entity, JObject json)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            if (json == null)
                throw new JsonMapperException("Missing JSON object!");

            OnBeforeApplyToEntity(entity, json);

            foreach (var entry in _configuration.Entries)
            {
                if ((entry.Mode & JsonMapperEntryMode.FromJson) != 0)
                    entry.MapToEntity(json, entity);
            }
        }

        /// <summary>
        /// Gets the difference between two entities
        /// </summary>
        /// <param name="source">Source entity to compare</param>
        /// <param name="target">Target entity to compare</param>
        /// <returns>JObject object with changed properties containing values from target entity</returns>
        public JObject Diff(T source, T target)
        {
            if (target == null)
                return null;
            if (source == null)
                return Map(target);

            var sourceJson = Map(source);
            var targetJson = Map(target);

            targetJson["data"].ToString();
            sourceJson["data"].ToString();

            var diff = new JObject();
            foreach (var property in targetJson.Properties())
            {
                if (!JToken.DeepEquals(sourceJson[property.Name], property.Value))
                {
                    diff.Add(property);
                }
            }
            return diff;
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Executed after entity is mapped to json object.
        /// Override to modify result json object.
        /// </summary>
        /// <param name="entity">Source entity object</param>
        /// <param name="json">Mapped json object</param>
        protected virtual void OnAfterMapToJson(T entity, JObject json)
        {
        }

        /// <summary>
        /// Executed before json object is applied to an entity.
        /// Override to modify source json before mapping.
        /// </summary>
        /// <param name="entity">Destination entity object</param>
        /// <param name="json">Source json object</param>
        protected virtual void OnBeforeApplyToEntity(T entity, JObject json)
        {
        }
        #endregion
    }
}