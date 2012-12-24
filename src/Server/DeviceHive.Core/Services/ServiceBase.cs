using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Data;

namespace DeviceHive.Core.Services
{
    /// <summary>
    /// Base class for DeviceHive buiseness services
    /// </summary>
    public abstract class ServiceBase
    {
        private readonly DataContext _dataContext;
        private readonly JsonMapperManager _jsonMapperManager;

        protected ServiceBase(DataContext dataContext, JsonMapperManager jsonMapperManager)
        {
            _dataContext = dataContext;
            _jsonMapperManager = jsonMapperManager;
        }

        protected DataContext DataContext
        {
            get { return _dataContext; }
        }

        protected IJsonMapper<T> GetMapper<T>()
        {
            return _jsonMapperManager.GetMapper<T>();
        }

        protected void Validate(object entity)
        {
            var result = new List<ValidationResult>();
            if (!Validator.TryValidateObject(entity, new ValidationContext(entity, null, null), result, true))
                throw new InvalidDataException(result.First().ErrorMessage);
        }
    }
}