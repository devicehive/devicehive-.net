using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a mapper that maps objects with <see cref="ParameterAttribute"/> to a dictionary.
    /// </summary>
    public static class ParameterMapper
    {
        #region Public Methods

        /// <summary>
        /// Maps an object with <see cref="ParameterAttribute"/> attributes to a dictionary.
        /// </summary>
        /// <param name="source">A source object with <see cref="ParameterAttribute"/>attributes.</param>
        /// <returns>An instance of dictionary with parameter values.</returns>
        public static Dictionary<string, object> Map(object source)
        {
            if (source == null)
                return null;

            var target = new Dictionary<string, object>();

            var sourceType = source.GetType();
            foreach (var property in sourceType.PrivateGetProperties().Where(p => p.IsDefined(typeof(ParameterAttribute), true)))
            {
                var parameterAttribute = property.GetAttributes<ParameterAttribute>().FirstOrDefault();
                var propertyValue = property.GetGetMethod(true).Invoke(source, new object[] { });
                if (propertyValue != null)
                {
                    target[parameterAttribute.Name] = propertyValue;
                }
            }

            return target;
        }

        /// <summary>
        /// Maps a dictionary to an object with <see cref="ParameterAttribute"/> attributes.
        /// </summary>
        /// <param name="source">A source dictionary with parameter values.</param>
        /// <param name="targetType">Type of the target object with <see cref="ParameterAttribute"/>attributes.</param>
        /// <returns>An instance of object with parameter values.</returns>
        public static object Map(Dictionary<string, object> source, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException("targetType");

            var target = Activator.CreateInstance(targetType);

            if (source != null && source.Any())
            {
                foreach (var property in targetType.PrivateGetProperties().Where(p => p.IsDefined(typeof(ParameterAttribute), true)))
                {
                    var parameterAttribute = property.GetAttributes<ParameterAttribute>().FirstOrDefault();

                    object value;
                    if (source.TryGetValue(parameterAttribute.Name, out value))
                    {
                        var mappedValue = TypeConverter.FromObject(value, property.PropertyType);
                        property.GetSetMethod(true).Invoke(target, new[] { mappedValue });
                    }
                }
            }

            return target;
        }
        #endregion
    }
}
