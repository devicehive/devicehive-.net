using System;
using System.Collections.Generic;
using System.Linq;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents static class for type conversions
    /// </summary>
    internal static class TypeConverter
    {
        #region Public Methods

        /// <summary>
        /// Converts an object into the specified type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <param name="type">Type to convert passed object to.</param>
        /// <returns>A converted object of specified type.</returns>
        public static object FromObject(object value, Type type)
        {
            if (value == null)
            {
                // for null values - return default value
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // for nullable types - get underlying type
                type = type.GetGenericArguments()[0];
            }

            if (type.IsEnum)
            {
                // for enum types - parse the name
                return Enum.Parse(type, value.ToString(), true);
            }
            if (Type.GetTypeCode(type) != TypeCode.Object)
            {
                // for simple types - convert value
                return System.Convert.ChangeType(value, type);
            }

            // check known non-simple types
            if (type == typeof(Type))
            {
                return Type.GetType(value.ToString(), true);
            }
            if (type == typeof(Guid))
            {
                return new Guid(value.ToString());
            }
            if (type == typeof(byte[]))
            {
                return System.Convert.FromBase64String(value.ToString());
            }

            throw new ArgumentException("Unsupported type: " + type);
        }

        /// <summary>
        /// Converts an object into the specified type.
        /// </summary>
        /// <typeparam name="T">Type to convert passed object to.</typeparam>
        /// <param name="value">Object to convert.</param>
        /// <returns>A converted object of specified type.</returns>
        public static T FromObject<T>(object value)
        {
            return (T)FromObject(value, typeof(T));
        }

        /// <summary>
        /// Converts a typed object into serializable object.
        /// </summary>
        /// <param name="value">A typed object to convert.</param>
        /// <returns>A serializable object.</returns>
        public static object ToObject(object value)
        {
            if (value == null)
            {
                return null;
            }

            var type = value.GetType();
            if (type == typeof(byte[]))
            {
                return System.Convert.ToBase64String((byte[])value);
            }

            return value;
        }
        #endregion
    }
}
