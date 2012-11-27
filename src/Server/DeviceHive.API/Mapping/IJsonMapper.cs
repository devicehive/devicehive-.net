using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Mapping
{
    public interface IJsonMapper
    {
        Type EntityType { get; }
        IList<IJsonMapperEntry> Entries { get; }
        
        JObject Map(object entity);
        object Map(JObject json);
        void Apply(object entity, JObject json);
    }

    public interface IJsonMapper<T>
    {
        JObject Map(T entity);
        T Map(JObject json);
        void Apply(T entity, JObject json);
    }

    public interface IJsonMapperEntry
    {
        JsonMapperEntryMode Mode { get; }
        string JsonProperty { get; }
        PropertyInfo EntityProperty { get; }
    }

    public enum JsonMapperEntryMode
    {
        OneWay,
        OneWayToSource,
        TwoWay,
    }
}