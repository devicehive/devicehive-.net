using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Data;

namespace DeviceHive.Core.Services
{
    /// <summary>
    /// Base class for DeviceHive buiseness services.
    /// </summary>
    public abstract class ServiceBase
    {
        private readonly DataContext _dataContext;
        private readonly JsonMapperManager _jsonMapperManager;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="dataContext">DataContext object.</param>
        /// <param name="jsonMapperManager">JsonMapperManager object.</param>
        protected ServiceBase(DataContext dataContext, JsonMapperManager jsonMapperManager)
        {
            _dataContext = dataContext;
            _jsonMapperManager = jsonMapperManager;
        }

        /// <summary>
        /// Gets DataContext object.
        /// </summary>
        protected DataContext DataContext
        {
            get { return _dataContext; }
        }

        /// <summary>
        /// Gets IJsonMapper implemetation.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <returns>IJsonMapper implemetatopn.</returns>
        protected IJsonMapper<T> GetMapper<T>()
        {
            return _jsonMapperManager.GetMapper<T>();
        }

        /// <summary>
        /// Validates the entity, throws InvalidDataException in case of validation errors.
        /// </summary>
        /// <param name="entity">Entity to validate.</param>
        protected void Validate(object entity)
        {
            var result = new List<ValidationResult>();
            if (!Validator.TryValidateObject(entity, new ValidationContext(entity, null, null), result, true))
                throw new InvalidDataException(result.First().ErrorMessage);
        }
    }
}