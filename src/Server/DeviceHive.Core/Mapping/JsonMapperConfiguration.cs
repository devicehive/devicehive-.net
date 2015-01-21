using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DeviceHive.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Core.Mapping
{
    /// <summary>
    /// Represents configuration for <see cref="JsonMapper{T}"/> class
    /// </summary> 
    /// <typeparam name="T">Object type</typeparam>
    public class JsonMapperConfiguration<T>
    {
        private readonly JsonMapperManager _manager;

        #region Public Properties

        /// <summary>
        /// Gets list of mapping entries
        /// </summary>
        public List<JsonMapperEntry<T>> Entries { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="manager">JsonMapperManager object</param>
        public JsonMapperConfiguration(JsonMapperManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");

            _manager = manager;

            Entries = new List<JsonMapperEntry<T>>();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Configures property mapping
        /// </summary>
        /// <param name="entityPropertyExpression">Property access expression</param>
        /// <param name="jsonProperty">Json property name</param>
        /// <param name="mode">Mapping mode</param>
        /// <returns>Current configuration object</returns>
        public JsonMapperConfiguration<T> Property(Expression<Func<T, object>> entityPropertyExpression, string jsonProperty, JsonMapperEntryMode mode = JsonMapperEntryMode.TwoWay)
        {
            if (entityPropertyExpression == null)
                throw new ArgumentNullException("entityPropertyExpression");
            if (string.IsNullOrEmpty(jsonProperty))
                throw new ArgumentException("jsonProperty is null or empty!", "jsonProperty");

            var entityProperty = GetProperty(entityPropertyExpression);

            // create MapToJson action
            var entityPropertyLambda = entityPropertyExpression.Compile();
            var entityConversionLambda = GetValueFormatter(entityProperty.PropertyType);
            Action<T, JObject> mapToJsonLabmda = (entity, json) =>
                {
                    var value = entityPropertyLambda(entity);
                    json.Add(new JProperty(jsonProperty, entityConversionLambda(value)));
                };

            // create MapToEntity action
            var entityPropertySetterLambda = GetPropertySetter(entityProperty);
            var jsonTokenParser = GetJsonTokenParser(entityProperty);
            Action<JObject, T, bool> mapToEntityLabmda = (json, entity, patch) =>
                {
                    var jProperty = json.Property(jsonProperty);
                    if (jProperty != null || !patch)
                    {
                        var value = jsonTokenParser(jsonProperty, jProperty != null ? jProperty.Value : null);
                        entityPropertySetterLambda(entity, value);
                    }
                };

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda, mapToEntityLabmda));
            return this;
        }

        /// <summary>
        /// Configures property mapping
        /// </summary>
        /// <param name="entityPropertyExpression">Property access expression</param>
        /// <param name="jsonProperty">Json property name</param>
        /// <param name="mode">Mapping mode</param>
        /// <returns>Current configuration object</returns>
        public JsonMapperConfiguration<T> EnumProperty<TEnum>(Expression<Func<T, int?>> entityPropertyExpression, string jsonProperty, JsonMapperEntryMode mode = JsonMapperEntryMode.TwoWay)
            where TEnum : struct
        {
            if (entityPropertyExpression == null)
                throw new ArgumentNullException("entityPropertyExpression");
            if (string.IsNullOrEmpty(jsonProperty))
                throw new ArgumentException("jsonProperty is null or empty!", "jsonProperty");
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException("TEnum is not an enum type!", "TEnum");

            var entityProperty = GetProperty(entityPropertyExpression);
            var isNullable = Nullable.GetUnderlyingType(entityProperty.PropertyType) != null;

            // create MapToJson action
            var entityPropertyLambda = entityPropertyExpression.Compile();
            var entityConversionLambda = GetValueFormatter(typeof(TEnum));
            Action<T, JObject> mapToJsonLabmda = (entity, json) =>
                {
                    var value = Enum.ToObject(typeof(TEnum), entityPropertyLambda(entity));
                    json.Add(new JProperty(jsonProperty, entityConversionLambda(value).ToString()));
                };

            // create MapToEntity action
            var entityPropertySetterLambda = GetPropertySetter(entityProperty);
            var jsonTokenParser = GetJsonTokenParser(entityProperty, isNullable ? typeof(Nullable<TEnum>) : typeof(TEnum));
            Action<JObject, T, bool> mapToEntityLabmda = (json, entity, patch) =>
                {
                    var jProperty = json.Property(jsonProperty);
                    if (jProperty != null || !patch)
                    {
                        var value = jsonTokenParser(jsonProperty, jProperty != null ? jProperty.Value : null);
                        entityPropertySetterLambda(entity, value);
                    }
                };

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda, mapToEntityLabmda));
            return this;
        }

        /// <summary>
        /// Configures raw json property.
        /// Raw json properties represent part of json structure.
        /// </summary>
        /// <param name="entityPropertyExpression">Property access expression</param>
        /// <param name="jsonProperty">Json property name</param>
        /// <param name="mode">Mapping mode</param>
        /// <returns>Current configuration object</returns>
        public JsonMapperConfiguration<T> RawJsonProperty(Expression<Func<T, string>> entityPropertyExpression, string jsonProperty, JsonMapperEntryMode mode = JsonMapperEntryMode.TwoWay)
        {
            if (entityPropertyExpression == null)
                throw new ArgumentNullException("entityPropertyExpression");
            if (string.IsNullOrEmpty(jsonProperty))
                throw new ArgumentException("jsonProperty is null or empty!", "jsonProperty");

            var entityProperty = GetProperty(entityPropertyExpression);

            // create MapToJson action
            var entityPropertyLambda = entityPropertyExpression.Compile();
            Action<T, JObject> mapToJsonLabmda = (entity, json) =>
                {
                    var value = entityPropertyLambda(entity);
                    json.Add(new JProperty(jsonProperty, value == null ? null : JToken.Parse(value)));
                };

            // create MapToEntity action
            var entityPropertySetterLambda = GetPropertySetter(entityProperty);
            Action<JObject, T, bool> mapToEntityLabmda = (json, entity, patch) =>
                {
                    var jProperty = json.Property(jsonProperty);
                    if (jProperty != null || !patch)
                    {
                        var value = jProperty == null || jProperty.Value.Type == JTokenType.Null ? null : jProperty.Value.ToString(Formatting.None);
                        entityPropertySetterLambda(entity, value);
                    }
                };

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda, mapToEntityLabmda));
            return this;
        }

        /// <summary>
        /// Configures reference property
        /// </summary>
        /// <typeparam name="TRef">Reference object type</typeparam>
        /// <param name="entityPropertyExpression">Property access expression</param>
        /// <param name="jsonProperty">Json property name</param>
        /// <param name="mode">Mapping mode</param>
        /// <returns>Current configuration object</returns>
        public JsonMapperConfiguration<T> ReferenceProperty<TRef>(Expression<Func<T, TRef>> entityPropertyExpression, string jsonProperty, JsonMapperEntryMode mode = JsonMapperEntryMode.TwoWay)
        {
            if (entityPropertyExpression == null)
                throw new ArgumentNullException("entityPropertyExpression");
            if (string.IsNullOrEmpty(jsonProperty))
                throw new ArgumentException("jsonProperty is null or empty!", "jsonProperty");

            var entityProperty = GetProperty(entityPropertyExpression);
            var mapper = _manager.GetMapper<TRef>();

            // create MapToJson action
            var entityPropertyLambda = entityPropertyExpression.Compile();
            Action<T, JObject> mapToJsonLabmda = (entity, json) =>
                {
                    var value = entityPropertyLambda(entity);
                    json.Add(new JProperty(jsonProperty, value == null ? null : mapper.Map(value)));
                };

            // create MapToEntity action
            var entityPropertySetterLambda = GetPropertySetter(entityProperty);
            Action<JObject, T, bool> mapToEntityLabmda = (json, entity, patch) =>
                {
                    var jProperty = json.Property(jsonProperty);
                    if (jProperty != null || !patch)
                    {
                        TRef refValue = default(TRef);
                        if (jProperty == null || jProperty.Value.Type == JTokenType.Null)
                        {
                            // null is passed - have to reset the foreign key property as well
                            var fkProperty = typeof(T).GetProperty(entityProperty.Name + "ID");
                            if (fkProperty != null)
                            {
                                fkProperty.SetValue(entity, null, null);
                            }
                        }
                        else if (jProperty.Value.Type == JTokenType.Object)
                        {
                            // apply the reference object
                            refValue = entityPropertyLambda(entity);
                            if (refValue == null || !patch)
                                refValue = (TRef)Activator.CreateInstance(typeof(TRef));
                            mapper.Apply(refValue, (JObject)jProperty.Value, patch);
                        }
                        else
                        {
                            throw new JsonMapperException("The value of the object property has invalid format, property: " + jsonProperty);
                        }
                        entityPropertySetterLambda(entity, refValue);
                    }
                };

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda, mapToEntityLabmda));
            return this;
        }
        
        /// <summary>
        /// Configures collection property
        /// </summary>
        /// <param name="entityPropertyExpression">Property access expression</param>
        /// <param name="jsonProperty">Json property name</param>
        /// <param name="mode">Mapping mode</param>
        /// <returns>Current configuration object</returns>
        public JsonMapperConfiguration<T> CollectionProperty<TRef>(Expression<Func<T, IList<TRef>>> entityPropertyExpression, string jsonProperty, JsonMapperEntryMode mode = JsonMapperEntryMode.TwoWay)
        {
            if (entityPropertyExpression == null)
                throw new ArgumentNullException("entityPropertyExpression");
            if (string.IsNullOrEmpty(jsonProperty))
                throw new ArgumentException("jsonProperty is null or empty!", "jsonProperty");

            var entityProperty = GetProperty(entityPropertyExpression);
            var mapper = _manager.GetMapper<TRef>();

            // create MapToJson action
            var entityPropertyLambda = entityPropertyExpression.Compile();
            Action<T, JObject> mapToJsonLabmda = (entity, json) =>
                {
                    var value = entityPropertyLambda(entity);
                    json.Add(new JProperty(jsonProperty, value == null ? null : value.Select(item => mapper.Map(item)).ToArray()));
                };

            // create MapToEntity action
            var entityPropertySetterLambda = GetPropertySetter(entityProperty);
            Action<JObject, T, bool> mapToEntityLabmda = (json, entity, patch) =>
                {
                    var jProperty = json.Property(jsonProperty);
                    if (jProperty != null || !patch)
                    {
                        var refValue = (IList<TRef>)null;
                        if (jProperty != null && jProperty.Value.Type == JTokenType.Array)
                        {
                            refValue = (IList<TRef>)Activator.CreateInstance(entityProperty.PropertyType);
                            foreach (var element in (JArray)jProperty.Value)
                            {
                                if (element.Type != JTokenType.Object)
                                    throw new JsonMapperException("The element of the collection property has invalid format, property: " + jsonProperty);

                                refValue.Add(mapper.Map((JObject)element));
                            }
                        }
                        else if (jProperty != null && jProperty.Value.Type != JTokenType.Null)
                        {
                            throw new JsonMapperException("The value of the collection property has invalid format, property: " + jsonProperty);
                        }
                        entityPropertySetterLambda(entity, refValue);
                    }
                };

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda, mapToEntityLabmda));
            return this;
        }
        #endregion

        #region Private Methods

        private PropertyInfo GetProperty<TRef>(Expression<Func<T, TRef>> entityProperty)
        {
            var body = ((LambdaExpression)entityProperty).Body;
            if (body.NodeType == ExpressionType.Convert)
            {
                body = ((UnaryExpression)body).Operand;
            }
            return (PropertyInfo)((MemberExpression)body).Member;
        }

        private Action<T, object> GetPropertySetter(PropertyInfo property)
        {
            var entityParameter = Expression.Parameter(typeof(T), "entity");
            var valueParameter = Expression.Parameter(typeof(object), "value");

            var expression = Expression.Assign(
                Expression.Property(entityParameter, property),
                Expression.Convert(valueParameter, property.PropertyType));
            return Expression.Lambda<Action<T, object>>(expression, entityParameter, valueParameter).Compile();
        }

        private Func<object, object> GetValueFormatter(Type propertyType)
        {
            var basePropertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (basePropertyType == typeof(Guid))
                return (Func<object, object>)(e => e == null ? null : ((Guid)e).ToString());

            return (Func<object, object>)(e => e);
        }

        private Func<string, JToken, object> GetJsonTokenParser(PropertyInfo property, Type type = null)
        {
            // returns a delegate that will parse JSON to a specified property

            type = type ?? property.PropertyType;

            if (type.IsArray)
            {
                return (Func<string, JToken, object>)((string propertyName, JToken jToken) =>
                    {
                        if (jToken == null || jToken.Type == JTokenType.Null)
                            return null;

                        if (jToken.Type != JTokenType.Array)
                            throw new JsonMapperException(string.Format("Invalid value in field '{0}', expected: Array, actual: {1}", propertyName, jToken.ToString(Formatting.None)));

                        var jArray = (JArray)jToken;
                        var elementType = type.GetElementType();
                        var elementParser = GetJsonTokenParser(property, elementType);
                        
                        var array = Array.CreateInstance(elementType, jArray.Count);
                        for (var i = 0; i < array.Length; i++)
                        {
                            array.SetValue(elementParser(propertyName + "[]", jArray[i]), i);
                        }
                        return array;
                    });
            }


            // define delegate for null testing: throw an exception if value is required, otherwise return a flag indicating if a value is null
            var basePropertyType = Nullable.GetUnderlyingType(type) ?? type;
            var hasRequredAttribute = property.IsDefined(typeof(RequiredAttribute));
            var defaultValueAttribute = (DefaultValueAttribute)property.GetCustomAttribute(typeof(DefaultValueAttribute));
            var isRequired = defaultValueAttribute == null && (hasRequredAttribute || (type.IsValueType && type == basePropertyType));
            var isNull = !isRequired ?
                (Func<string, JToken, bool>)((string propertyName, JToken jToken) => jToken == null || jToken.Type == JTokenType.Null) :
                (Func<string, JToken, bool>)((string propertyName, JToken jToken) =>
                    {
                        if (jToken == null)
                            throw new JsonMapperException(string.Format("The '{0}' field is required!", propertyName));
                        if (jToken.Type == JTokenType.Null)
                            throw new JsonMapperException(string.Format("The '{0}' field cannot be null!", propertyName));

                        return false;
                    });

            if (basePropertyType == typeof(Guid))
            {
                // return a delegate that pases JSON to Guid
                return (Func<string, JToken, object>)((string propertyName, JToken jToken) =>
                    {
                        if (isNull(propertyName, jToken))
                            return null;

                        if (jToken.Type != JTokenType.String)
                            throw new JsonMapperException(string.Format("Invalid value in field '{0}', expected: Guid, actual: {1}", propertyName, jToken.ToString(Formatting.None)));

                        try
                        {
                            return Guid.Parse((string)jToken);
                        }
                        catch (FormatException)
                        {
                            throw new JsonMapperException(string.Format("Invalid value in field '{0}', expected: Guid, actual: {1}", propertyName, jToken.ToString(Formatting.None)));
                        }
                    });
            }
            else if (basePropertyType.IsEnum)
            {
                // return a delegate that pases JSON to Enum (either by name or integer equivalent)
                return (Func<string, JToken, object>)((string propertyName, JToken jToken) =>
                    {
                        if (isNull(propertyName, jToken))
                            return defaultValueAttribute != null ? defaultValueAttribute.Value : null;

                        if (jToken.Type == JTokenType.String)
                        {
                            try
                            {
                                return Enum.Parse(basePropertyType, (string)jToken, true);
                            }
                            catch (ArgumentException)
                            {
                                throw new JsonMapperException(string.Format("Invalid enumeration value in field '{0}': {1}", propertyName, jToken.ToString(Formatting.None)));
                            }
                        }
                        else if (jToken.Type == JTokenType.Integer)
                        {
                            return Enum.ToObject(basePropertyType, (int)jToken);
                        }
                        else
                        {
                            throw new JsonMapperException(string.Format("Invalid value in field '{0}', expected: Enum, actual: {1}", propertyName, jToken.ToString(Formatting.None)));
                        }
                    });
            }
            else
            {
                // return a delegate that pases JSON to other types (integer, float, boolean, string, date)
                return (Func<string, JToken, object>)((string propertyName, JToken jToken) =>
                    {
                        if (isNull(propertyName, jToken))
                            return defaultValueAttribute != null ? defaultValueAttribute.Value : null;

                        try
                        {
                            return jToken.ToObject(basePropertyType);
                        }
                        catch (ArgumentException)
                        {
                            throw new JsonMapperException(string.Format("Invalid value in field '{0}', expected: {1}, actual: {2}",
                                propertyName, basePropertyType.Name, jToken.ToString(Formatting.None)));
                        }
                        catch (FormatException)
                        {
                            throw new JsonMapperException(string.Format("Invalid value in field '{0}', expected: {1}, actual: {2}",
                                propertyName, basePropertyType.Name, jToken.ToString(Formatting.None)));
                        }
                    });
            }
        }
        #endregion
    }
}