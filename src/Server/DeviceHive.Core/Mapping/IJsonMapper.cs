using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Core.Mapping
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
        /// <param name="patch">Whether to ignore absense of required fields</param>
        void Apply(object entity, JObject json, bool patch);
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
        /// <param name="oneWayOnly">Whether to map only OneWay fields</param>
        /// <returns>Mapped JObject object</returns>
        JObject Map(T entity, bool oneWayOnly = false);

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
        /// <param name="patch">Whether to ignore absense of required fields</param>
        void Apply(T entity, JObject json, bool patch = true);

        /// <summary>
        /// Gets the difference between two entities
        /// </summary>
        /// <param name="source">Source entity to compare</param>
        /// <param name="target">Target entity to compare</param>
        /// <returns>JObject object with changed properties containing values from target entity</returns>
        JObject Diff(T source, T target);
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
    [Flags]
    public enum JsonMapperEntryMode
    {
        /// <summary>
        /// Mapping from object to json
        /// </summary>
        ToJson = 1,

        /// <summary>
        /// Mapping from json to object
        /// </summary>
        FromJson = 2,

        /// <summary>
        /// Mapping in both directions
        /// </summary>
        TwoWay = 1 + 2,

        /// <summary>
        /// Mapping in one direction only
        /// This value is assigned automatically and used just for checks
        /// </summary>
        OneWayOnly = 4,
    }
}