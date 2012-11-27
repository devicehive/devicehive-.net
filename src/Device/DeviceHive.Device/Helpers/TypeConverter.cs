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
        /// Parses a string into the specified type.
        /// </summary>
        /// <param name="value">String to parse.</param>
        /// <param name="type">Type to parse specified string to.</param>
        /// <returns>Parsed object.</returns>
        public static object Parse(string value, Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null) return null;
                type = type.GetGenericArguments()[0];
            }
            if (type.IsEnum)
            {
                // for enum types - parse the name
                return Enum.Parse(type, value, true);
            }
            if (Type.GetTypeCode(type) != TypeCode.Object)
            {
                // for simple types - convert value
                return Convert.ChangeType(value, type);
            }

            // check known non-simple types
            if (type == typeof(Type))
            {
                return Type.GetType(value, true);
            }
            if (type == typeof(Guid))
            {
                return new Guid(value);
            }
            throw new ArgumentException("Unsupported type: " + type);
        }

        /// <summary>
        /// Parses a string into the specified type.
        /// </summary>
        /// <typeparam name="T">Type to parse specified string to.</typeparam>
        /// <param name="value">String to parse.</param>
        /// <returns>Parsed object.</returns>
        public static T Parse<T>(string value)
        {
            return (T)Parse(value, typeof(T));
        }
        #endregion
    }
}
