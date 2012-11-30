using System;
using System.Collections.Generic;
using System.Linq;

namespace DeviceHive.API.Mapping
{
    /// <summary>
    /// Represents container for json mappers
    /// </summary>
    public class JsonMapperManager
    {
        private readonly Dictionary<Type, object> _configurations = new Dictionary<Type, object>();

        #region Public Methods

        /// <summary>
        /// Adds json mapper instance to the container
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapper">Json mapper instance</param>
        public void AddMapper<T>(IJsonMapper<T> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException("mapper");

            _configurations[typeof(T)] = mapper;
        }

        /// <summary>
        /// Gets list of mapped types
        /// </summary>
        /// <returns>Array of Type objects</returns>
        public Type[] GetTypes()
        {
            return _configurations.Keys.ToArray();
        }

        /// <summary>
        /// Gets mapper instance
        /// </summary>
        /// <param name="type">Object type</param>
        /// <returns>Mapper implementation</returns>
        public IJsonMapper GetMapper(Type type)
        {
            object obj;
            if (!_configurations.TryGetValue(type, out obj))
            {
                throw new InvalidOperationException(string.Format(
                    "Mapping configuration for object of type {0} was not specified!", type.FullName));
            }
            return (IJsonMapper)obj;
        }

        /// <summary>
        /// Gets mapper instance
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <returns>Mapper implementation</returns>
        public IJsonMapper<T> GetMapper<T>()
        {
            object obj;
            if (!_configurations.TryGetValue(typeof(T), out obj))
            {
                throw new InvalidOperationException(string.Format(
                    "Mapping configuration for object of type {0} was not specified!", typeof(T).FullName));
            }
            return (IJsonMapper<T>)obj;
        }
        #endregion
    }
}