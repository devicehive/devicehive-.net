using System;
using System.Collections.Generic;
using System.Linq;

namespace DeviceHive.API.Mapping
{
    public class JsonMapperManager
    {
        private Dictionary<Type, object> _configurations = new Dictionary<Type, object>();

        #region Public Properties

        public DataContext DataContext { get; private set; }

        #endregion

        #region Constructor

        public JsonMapperManager(DataContext dataContext)
        {
            if (dataContext == null)
                throw new ArgumentNullException("dataContext");

            DataContext = dataContext;
        }
        #endregion

        #region Public Methods

        public void Configure<T>(IJsonMapper<T> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException("mapper");

            _configurations[typeof(T)] = mapper;
        }

        public Type[] GetTypes()
        {
            return _configurations.Keys.ToArray();
        }

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