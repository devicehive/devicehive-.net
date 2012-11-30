using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Mapping
{
    /// <summary>
    /// Represents non-generic interface for json mappers
    /// </summary>
    public interface IJsonMapper
    {
        /// <summary>
        /// Gets mapper entity type
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Gets list of mapping entries
        /// </summary>
        IList<IJsonMapperEntry> Entries { get; }
        
        /// <summary>
        /// Maps entity to json
        /// </summary>
        /// <param name="entity">Entity object</param>
        /// <returns>Mapped JObject object</returns>
        JObject Map(object entity);

        /// <summary>
        /// Maps json to entity
        /// </summary>
        /// <param name="json">JObject object</param>
        /// <returns>Mapped entity object</returns>
        object Map(JObject json);

        /// <summary>
        /// Applies json fields to entity
        /// </summary>
        /// <param name="entity">Destination entity object</param>
        /// <param name="json">JObject object to apply</param>
        void Apply(object entity, JObject json);
    }

    /// <summary>
    /// Represents generic interface for json mappers
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IJsonMapper<T>
    {
        /// <summary>
        /// Maps entity to json
        /// </summary>
        /// <param name="entity">Entity object</param>
        /// <returns>Mapped JObject object</returns>
        JObject Map(T entity);

        /// <summary>
        /// Maps json to entity
        /// </summary>
        /// <param name="json">JObject object</param>
        /// <returns>Mapped entity object</returns>
        T Map(JObject json);

        /// <summary>
        /// Applies json fields to entity
        /// </summary>
        /// <param name="entity">Destination entity object</param>
        /// <param name="json">JObject object to apply</param>
        void Apply(T entity, JObject json);
    }

    /// <summary>
    /// Represents mapping entry
    /// </summary>
    public interface IJsonMapperEntry
    {
        /// <summary>
        /// Gets entry mapping mode
        /// </summary>
        JsonMapperEntryMode Mode { get; }

        /// <summary>
        /// Gets name of corresponding json property
        /// </summary>
        string JsonProperty { get; }

        /// <summary>
        /// Gets corresponding domain object property
        /// </summary>
        PropertyInfo EntityProperty { get; }
    }

    /// <summary>
    /// Represents available mapping modes
    /// </summary>
    public enum JsonMapperEntryMode
    {
        /// <summary>
        /// Mapping from object to json only
        /// </summary>
        OneWay,

        /// <summary>
        /// Mapping from json to object only
        /// </summary>
        OneWayToSource,

        /// <summary>
        /// Mapping in both directions
        /// </summary>
        TwoWay,
    }
}