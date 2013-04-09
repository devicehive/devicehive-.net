using System;
using System.Collections.Generic;
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
        private readonly DataContext _dataContext;

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
        /// <param name="dataContext">DataContext object</param>
        public JsonMapperConfiguration(JsonMapperManager manager, DataContext dataContext)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");
            if (dataContext == null)
                throw new ArgumentNullException("dataContext");

            _manager = manager;
            _dataContext = dataContext;

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

            var entity = Expression.Parameter(typeof(T), "entity");
            var json = Expression.Parameter(typeof(JObject), "json");
            var entityProperty = GetProperty(entityPropertyExpression);

            // create MapToJson action
            var entityGetValue = (Expression)Expression.Invoke(entityPropertyExpression, entity);
            if (entityProperty.PropertyType == typeof(Guid))
            {
                entityGetValue = Expression.Call(Expression.Convert(entityGetValue, typeof(Guid)),
                    typeof(Guid).GetMethod("ToString", new Type[] { }));
            }
            var jsonAddProperty = Expression.Call(json,
                typeof(JObject).GetMethod("Add", new[] { typeof(JProperty) }),
                Expression.New(typeof(JProperty).GetConstructor(new[] { typeof(string), typeof(object) }),
                    Expression.Constant(jsonProperty), entityGetValue));
            var mapToJsonLabmda = Expression.Lambda<Action<T, JObject>>(jsonAddProperty, entity, json);

            // create MapToEntity action
            var jsonPropertyCall = Expression.Call(json, typeof(JObject).GetMethod("Property"), Expression.Constant(jsonProperty));
            var jsonParseValue = (Expression)null;
            if (entityProperty.PropertyType == typeof(Guid))
            {
                jsonParseValue = Expression.Call(null, typeof(Guid).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static),
                    Expression.Convert(Expression.Property(jsonPropertyCall, "Value"), typeof(string)));
            }
            else if (entityProperty.PropertyType.IsEnum)
            {
                jsonParseValue = Expression.Convert(
                    Expression.Call(null, typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) }),
                        Expression.Constant(entityProperty.PropertyType),
                        Expression.Convert(Expression.Property(jsonPropertyCall, "Value"), typeof(string)),
                        Expression.Constant(true)),
                    entityProperty.PropertyType);
            }
            else
            {
                jsonParseValue = Expression.Convert(Expression.Property(jsonPropertyCall, "Value"), entityProperty.PropertyType);
            }
            var entityAssign = Expression.IfThen(
                Expression.NotEqual(jsonPropertyCall, Expression.Constant(null, typeof(JProperty))),
                Expression.Assign(Expression.Property(entity, entityProperty), jsonParseValue));
            var mapToEntityLabmda = Expression.Lambda<Action<JObject, T>>(entityAssign, json, entity);

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda.Compile(), mapToEntityLabmda.Compile()));
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

            var entity = Expression.Parameter(typeof(T), "entity");
            var json = Expression.Parameter(typeof(JObject), "json");
            var entityProperty = GetProperty(entityPropertyExpression);

            // create MapToJson action
            var jsonAddProperty = Expression.Call(json,
                typeof(JObject).GetMethod("Add", new[] { typeof(JProperty) }),
                Expression.New(typeof(JProperty).GetConstructor(new[] { typeof(string), typeof(object) }),
                    Expression.Constant(jsonProperty),
                    Expression.Condition(
                        Expression.NotEqual(Expression.Invoke(entityPropertyExpression, entity), Expression.Constant(null, typeof(string))),
                        Expression.Call(null, typeof(JToken).GetMethod("Parse"), Expression.Invoke(entityPropertyExpression, entity)),
                        Expression.Constant(null, typeof(JToken)))));
            var mapToJsonLabmda = Expression.Lambda<Action<T, JObject>>(jsonAddProperty, entity, json);

            // create MapToEntity action
            var jsonPropertyCall = Expression.Call(json, typeof(JObject).GetMethod("Property"), Expression.Constant(jsonProperty));
            var entityAssign = Expression.IfThen(
                Expression.NotEqual(jsonPropertyCall, Expression.Constant(null, typeof(JProperty))),
                Expression.Assign(
                    Expression.Property(entity, entityProperty),
                    Expression.Condition(
                        Expression.AndAlso(
                            Expression.TypeIs(Expression.Property(jsonPropertyCall, "Value"), typeof(JValue)),
                            Expression.Equal(
                                Expression.Property(Expression.Convert(Expression.Property(jsonPropertyCall, "Value"), typeof(JValue)), "Value"),
                                Expression.Constant(null, typeof(object)))),
                        Expression.Constant(null, typeof(string)),
                        Expression.Call(Expression.Property(jsonPropertyCall, "Value"),
                            typeof(JToken).GetMethod("ToString", new Type[] { typeof(Formatting), typeof(JsonConverter[]) }),
                            Expression.Constant(Formatting.None), Expression.Constant(new JsonConverter[] { })))));
            var mapToEntityLabmda = Expression.Lambda<Action<JObject, T>>(entityAssign, json, entity);

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda.Compile(), mapToEntityLabmda.Compile()));
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

            var entity = Expression.Parameter(typeof(T), "entity");
            var json = Expression.Parameter(typeof(JObject), "json");
            var entityProperty = GetProperty(entityPropertyExpression);
            var mapper = _manager.GetMapper<TRef>();
            var repository = _dataContext.GetRepositoryFor<TRef>();

            // create MapToJson action
            var jsonAddProperty = Expression.Call(json,
                typeof(JObject).GetMethod("Add", new[] { typeof(JProperty) }),
                Expression.New(typeof(JProperty).GetConstructor(new[] { typeof(string), typeof(object) }),
                    Expression.Constant(jsonProperty),
                    Expression.Condition(
                        Expression.NotEqual(Expression.Invoke(entityPropertyExpression, entity), Expression.Constant(null, typeof(TRef))),
                        Expression.Call(Expression.Constant(mapper), typeof(IJsonMapper<>).MakeGenericType(typeof(TRef))
                            .GetMethod("Map", new[] { typeof(TRef), typeof(bool) }), Expression.Invoke(entityPropertyExpression, entity), Expression.Constant(false)),
                        Expression.Constant(null, typeof(JObject)))));
            var mapToJsonLabmda = Expression.Lambda<Action<T, JObject>>(jsonAddProperty, entity, json);

            // create MapToEntity action - use delegate for simplicity
            Action<JObject, T> mapToEntityLabmda = (json2, entity2) =>
            {
                var jProperty = json2.Property(jsonProperty);
                if (jProperty == null)
                    return;

                TRef refValue = default(TRef);
                var jValue = jProperty.Value as JValue;
                if (jValue != null && jValue.Type == JTokenType.Null)
                {
                    // null is passed - have to reset the foreign key property as well
                    var fkProperty = typeof(T).GetProperty(entityProperty.Name + "ID");
                    if (fkProperty != null)
                    {
                        fkProperty.SetValue(entity2, null, null);
                    }
                }
                else if (jValue != null && (jValue.Value is long))
                {
                    // search object by ID
                    refValue = repository.Get((int)jValue);
                    if (refValue == null)
                    {
                        throw new JsonMapperException(string.Format("ID of the reference property is not found! " +
                            "Property: {0}, ID: {1}", jsonProperty, jValue));
                    }
                }
                else
                {
                    throw new JsonMapperException(string.Format("The required reference property has invalid format! " +
                        "Property: {0}", jsonProperty));
                }
                entityProperty.SetValue(entity2, refValue, null);
            };

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda.Compile(), mapToEntityLabmda));
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
        #endregion
    }
}