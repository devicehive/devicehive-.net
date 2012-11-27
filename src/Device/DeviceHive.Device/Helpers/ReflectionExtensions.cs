using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents static class with reflection extensions.
    /// </summary>
    internal static class ReflectionExtensioins
    {
        #region Public Methods (PropertyInfo)

        /// <summary>
        /// Gets public property.
        /// </summary>
        /// <param name="type">Type to get property on.</param>
        /// <param name="property">Property name.</param>
        /// <returns>PropertyInfo object.</returns>
        public static PropertyInfo PublicGetProperty(this Type type, string property)
        {
            return type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Gets public properties.
        /// </summary>
        /// <param name="type">Type to get properties on.</param>
        /// <returns>Array of PropertyInfo objects.</returns>
        public static PropertyInfo[] PublicGetProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Gets private property.
        /// </summary>
        /// <param name="type">Type to get property on.</param>
        /// <param name="property">Property name.</param>
        /// <returns>PropertyInfo object.</returns>
        public static PropertyInfo PrivateGetProperty(this Type type, string property)
        {
            // iterate through inheritance hierarchy,
            // since private properties could only be retrieved from type that declares it
            for (var t = type; t != null; t = t.BaseType)
            {
                var propertyInfo = t.GetProperty(property,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (propertyInfo != null) return propertyInfo;
            }
            return null;
        }

        /// <summary>
        /// Gets private properties.
        /// </summary>
        /// <param name="type">Type to get properties on.</param>
        /// <returns>Array of PropertyInfo objects.</returns>
        public static PropertyInfo[] PrivateGetProperties(this Type type)
        {
            // iterate through inheritance hierarchy,
            // since private properties could only be retrieved from type that declares it
            var propertyInfos = new List<PropertyInfo>();
            for (var t = type; t != null; t = t.BaseType)
            {
                propertyInfos.AddRange(t.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            }
            return propertyInfos.ToArray();
        }

        /// <summary>
        /// Gets static property.
        /// </summary>
        /// <param name="type">Type to get property on.</param>
        /// <param name="property">Property name.</param>
        /// <returns>PropertyInfo object.</returns>
        public static PropertyInfo StaticGetProperty(this Type type, string property)
        {
            return type.GetProperty(property, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Gets static properties.
        /// </summary>
        /// <param name="type">Type to get properties on.</param>
        /// <returns>Array of PropertyInfo objects.</returns>
        public static PropertyInfo[] StaticGetProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        #endregion

        #region Public Methods (MethodInfo)

        /// <summary>
        /// Gets public method.
        /// </summary>
        /// <param name="type">Type to get method on.</param>
        /// <param name="method">Method name.</param>
        /// <returns>MethodInfo object.</returns>
        public static MethodInfo PublicGetMethod(this Type type, string method)
        {
            return type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Gets public methods.
        /// </summary>
        /// <param name="type">Type to get methods on.</param>
        /// <returns>Array of MethodInfo objects.</returns>
        public static MethodInfo[] PublicGetMethods(this Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Gets private method.
        /// </summary>
        /// <param name="type">Type to get method on.</param>
        /// <param name="method">Method name.</param>
        /// <returns>MethodInfo object.</returns>
        public static MethodInfo PrivateGetMethod(this Type type, string method)
        {
            // iterate through inheritance hierarchy,
            // since private methods could only be retrieved from type that declares it
            for (var t = type; t != null; t = t.BaseType)
            {
                var methodInfo = t.GetMethod(method,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (methodInfo != null) return methodInfo;
            }
            return null;
        }

        /// <summary>
        /// Gets private methods.
        /// </summary>
        /// <param name="type">Type to get methods on.</param>
        /// <returns>Array of MethodInfo objects.</returns>
        public static MethodInfo[] PrivateGetMethods(this Type type)
        {
            // iterate through inheritance hierarchy,
            // since private methods could only be retrieved from type that declares it
            var methodInfos = new List<MethodInfo>();
            for (var t = type; t != null; t = t.BaseType)
            {
                methodInfos.AddRange(t.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            }
            return methodInfos.ToArray();
        }

        /// <summary>
        /// Gets static method.
        /// </summary>
        /// <param name="type">Type to get method on.</param>
        /// <param name="method">Method name.</param>
        /// <returns>MethodInfo object.</returns>
        public static MethodInfo StaticGetMethod(this Type type, string method)
        {
            return type.GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Gets static methods.
        /// </summary>
        /// <param name="type">Type to get methods on.</param>
        /// <returns>Array of MethodInfo objects.</returns>
        public static MethodInfo[] StaticGetMethods(this Type type)
        {
            return type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        #endregion

        #region Public Methods (FieldInfo)

        /// <summary>
        /// Gets public field.
        /// </summary>
        /// <param name="type">Type to get field on.</param>
        /// <param name="field">Field name.</param>
        /// <returns>FieldInfo object.</returns>
        public static FieldInfo PublicGetField(this Type type, string field)
        {
            return type.GetField(field, BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Gets public fields.
        /// </summary>
        /// <param name="type">Type to get fields on.</param>
        /// <returns>Array of FieldInfo objects.</returns>
        public static FieldInfo[] PublicGetFields(this Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Gets private field.
        /// </summary>
        /// <param name="type">Type to get field on.</param>
        /// <param name="field">Field name.</param>
        /// <returns>FieldInfo object.</returns>
        public static FieldInfo PrivateGetField(this Type type, string field)
        {
            // iterate through inheritance hierarchy,
            // since private fields could only be retrieved from type that declares it
            for (var t = type; t != null; t = t.BaseType)
            {
                var fieldInfo = t.GetField(field,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (fieldInfo != null) return fieldInfo;
            }
            return null;
        }

        /// <summary>
        /// Gets private fields.
        /// </summary>
        /// <param name="type">Type to get fields on.</param>
        /// <returns>Array of FieldInfo objects.</returns>
        public static FieldInfo[] PrivateGetFields(this Type type)
        {
            // iterate through inheritance hierarchy,
            // since private fields could only be retrieved from type that declares it
            var fieldInfos = new List<FieldInfo>();
            for (var t = type; t != null; t = t.BaseType)
            {
                fieldInfos.AddRange(t.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            }
            return fieldInfos.ToArray();
        }

        /// <summary>
        /// Gets static field.
        /// </summary>
        /// <param name="type">Type to get field on.</param>
        /// <param name="field">Field name.</param>
        /// <returns>FieldInfo object.</returns>
        public static FieldInfo StaticGetField(this Type type, string field)
        {
            return type.GetField(field, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Gets static fields.
        /// </summary>
        /// <param name="type">Type to get fields on.</param>
        /// <returns>Array of FieldInfo objects.</returns>
        public static FieldInfo[] StaticGetFields(this Type type)
        {
            return type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        #endregion

        #region Public Methods (Attributes)

        /// <summary>
        /// Gets attributes defined on the type.
        /// </summary>
        /// <typeparam name="T">Type of the attribute to get.</typeparam>
        /// <param name="type">Type to get attributes for.</param>
        /// <returns>List of attributes.</returns>
        public static List<T> GetAttributes<T>(this Type type)
            where T : Attribute
        {
            return type.GetAttributes<T>(true);
        }

        /// <summary>
        /// Gets attributes defined on the type.
        /// </summary>
        /// <typeparam name="T">Type of the attribute to get.</typeparam>
        /// <param name="type">Type to get attributes for.</param>
        /// <param name="inherit">Whether to search member's inheritance chain.</param>
        /// <returns>List of attributes.</returns>
        public static List<T> GetAttributes<T>(this Type type, bool inherit)
        {
            return type.GetCustomAttributes(typeof(T), inherit).Cast<T>().ToList();
        }

        /// <summary>
        /// Gets attributes defined on the member.
        /// </summary>
        /// <typeparam name="T">Type of the attribute to get.</typeparam>
        /// <param name="member">Member to get attributes for.</param>
        /// <returns>List of attributes.</returns>
        public static List<T> GetAttributes<T>(this MemberInfo member)
            where T : Attribute
        {
            return member.GetAttributes<T>(true);
        }

        /// <summary>
        /// Gets attributes defined on the member.
        /// </summary>
        /// <typeparam name="T">Type of the attribute to get.</typeparam>
        /// <param name="member">Member to get attributes for.</param>
        /// <param name="inherit">Whether to search member's inheritance chain.</param>
        /// <returns>List of attributes.</returns>
        public static List<T> GetAttributes<T>(this MemberInfo member, bool inherit)
        {
            return member.GetCustomAttributes(typeof(T), inherit).Cast<T>().ToList();
        }
        #endregion
    }
}
