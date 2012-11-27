using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;
using DeviceHive.Data.Repositories;

namespace DeviceHive.API.Mapping
{
    public class JsonMapper<T> : IJsonMapper, IJsonMapper<T>
    {
        private JsonMapperConfiguration<T> _configuration;

        #region Constructor

        public JsonMapper(JsonMapperConfiguration<T> configuration)
        {
            _configuration = configuration;
        }
        #endregion

        #region IJsonMapper Members

        Type IJsonMapper.EntityType
        {
            get { return typeof(T); }
        }

        IList<IJsonMapperEntry> IJsonMapper.Entries
        {
            get { return _configuration.Entries.Cast<IJsonMapperEntry>().ToList(); }
        }

        JObject IJsonMapper.Map(object entity)
        {
            if (!typeof(T).IsInstanceOfType(entity))
                throw new ArgumentException(string.Format("Entity type is invalid! Expected: {0}, Actual: {1}", typeof(T), entity.GetType()), "entity");

            return Map((T)entity);
        }

        object IJsonMapper.Map(JObject json)
        {
            return Map(json);
        }

        void IJsonMapper.Apply(object entity, JObject json)
        {
            if (!typeof(T).IsInstanceOfType(entity))
                throw new ArgumentException(string.Format("Entity type is invalid! Expected: {0}, Actual: {1}", typeof(T), entity.GetType()), "entity");

            Apply((T)entity, json);
        }
        #endregion

        #region IJsonMapper<T> Members

        public JObject Map(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var json = new JObject();
            foreach (var entry in _configuration.Entries)
            {
                if (entry.Mode == JsonMapperEntryMode.OneWay || entry.Mode == JsonMapperEntryMode.TwoWay)
                    entry.MapToJson(entity, json);
            }
            return json;
        }

        public T Map(JObject json)
        {
            var entity = (T)Activator.CreateInstance(typeof(T));
            Apply(entity, json);
            return entity;
        }

        public void Apply(T entity, JObject json)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            if (json == null)
                throw new ArgumentNullException("json");

            foreach (var entry in _configuration.Entries)
            {
                if (entry.Mode == JsonMapperEntryMode.OneWayToSource || entry.Mode == JsonMapperEntryMode.TwoWay)
                    entry.MapToEntity(json, entity);
            }
        }
        #endregion
    }

    public class JsonMapperConfiguration<T>
    {
        private JsonMapperManager _manager;
        private DataContext _dataContext;

        #region Public Properties

        public List<JsonMapperEntry<T>> Entries { get; private set; }

        #endregion

        #region Constructor

        public JsonMapperConfiguration(JsonMapperManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");

            _manager = manager;
            _dataContext = manager.DataContext;
            
            Entries = new List<JsonMapperEntry<T>>();
        }
        #endregion

        #region Public Methods

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
            var jsonAddProperty = Expression.Call(json,
                typeof(JObject).GetMethod("Add", new[] { typeof(JProperty) }),
                Expression.New(typeof(JProperty).GetConstructor(new[] { typeof(string), typeof(object) }),
                    Expression.Constant(jsonProperty),
                    Expression.Invoke(entityPropertyExpression, entity)));
            var mapToJsonLabmda = Expression.Lambda<Action<T, JObject>>(jsonAddProperty, entity, json);

            // create MapToEntity action
            var jsonPropertyCall = Expression.Call(json, typeof(JObject).GetMethod("Property"), Expression.Constant(jsonProperty));
            var entityAssign = Expression.IfThen(
                Expression.NotEqual(jsonPropertyCall, Expression.Constant(null, typeof(JProperty))),
                Expression.Assign(
                    Expression.Property(entity, entityProperty),
                    Expression.Convert(Expression.Property(jsonPropertyCall, "Value"), entityProperty.PropertyType)));
            var mapToEntityLabmda = Expression.Lambda<Action<JObject, T>>(entityAssign, json, entity);

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda.Compile(), mapToEntityLabmda.Compile()));
            return this;
        }

        public JsonMapperConfiguration<T> Property(Expression<Func<T, Guid>> entityPropertyExpression, string jsonProperty, JsonMapperEntryMode mode = JsonMapperEntryMode.TwoWay)
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
                    Expression.Call(Expression.Invoke(entityPropertyExpression, entity), typeof(Guid).GetMethod("ToString", new Type[] { }))));
            var mapToJsonLabmda = Expression.Lambda<Action<T, JObject>>(jsonAddProperty, entity, json);

            // create MapToEntity action
            var jsonPropertyCall = Expression.Call(json, typeof(JObject).GetMethod("Property"), Expression.Constant(jsonProperty));
            var entityAssign = Expression.IfThen(
                Expression.NotEqual(jsonPropertyCall, Expression.Constant(null, typeof(JProperty))),
                Expression.Assign(
                    Expression.Property(entity, entityProperty),
                    Expression.Call(null, typeof(Guid).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static),
                        Expression.Convert(Expression.Property(jsonPropertyCall, "Value"), typeof(string)))));
            var mapToEntityLabmda = Expression.Lambda<Action<JObject, T>>(entityAssign, json, entity);

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda.Compile(), mapToEntityLabmda.Compile()));
            return this;
        }

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
                        Expression.Call(Expression.Property(jsonPropertyCall, "Value"), typeof(JToken).GetMethod("ToString", new Type[] { })))));
            var mapToEntityLabmda = Expression.Lambda<Action<JObject, T>>(entityAssign, json, entity);

            Entries.Add(new JsonMapperEntry<T>(mode, jsonProperty, entityProperty, mapToJsonLabmda.Compile(), mapToEntityLabmda.Compile()));
            return this;
        }

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
            var repository = _dataContext.Get<TRef>();

            // create MapToJson action
            var jsonAddProperty = Expression.Call(json,
                typeof(JObject).GetMethod("Add", new[] { typeof(JProperty) }),
                Expression.New(typeof(JProperty).GetConstructor(new[] { typeof(string), typeof(object) }),
                    Expression.Constant(jsonProperty),
                    Expression.Condition(
                        Expression.NotEqual(Expression.Invoke(entityPropertyExpression, entity), Expression.Constant(null, typeof(TRef))),
                        Expression.Call(Expression.Constant(mapper), typeof(IJsonMapper<>).MakeGenericType(typeof(TRef))
                            .GetMethod("Map", new[] { typeof(TRef) }), Expression.Invoke(entityPropertyExpression, entity)),
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
                if (jValue != null && (jValue.Value is long))
                {
                    // search object by ID
                    refValue = repository.Get((int)jValue);
                    if (refValue == null)
                    {
                        throw new JsonMapperException(string.Format("ID of the reference property is not found! " +
                            "Property: {0}, ID: {1}", jsonProperty, jValue));
                    }
                    entityProperty.SetValue(entity2, refValue, null);
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

    public class JsonMapperEntry<T> : IJsonMapperEntry
    {
        private Action<T, JObject> _mapToJsonAction;
        private Action<JObject, T> _mapToEntityAction;

        #region IJsonMapperEntry Members

        public JsonMapperEntryMode Mode { get; private set; }
        public string JsonProperty { get; private set; }
        public PropertyInfo EntityProperty { get; private set; }

        #endregion

        #region Constructor

        public JsonMapperEntry(JsonMapperEntryMode mode, string jsonProperty, PropertyInfo entityProperty,
            Action<T, JObject> mapToJsonAction, Action<JObject, T> mapToEntityAction)
        {
            Mode = mode;
            JsonProperty = jsonProperty;
            EntityProperty = entityProperty;

            _mapToJsonAction = mapToJsonAction;
            _mapToEntityAction = mapToEntityAction;
        }
        #endregion

        #region Public Methods

        public virtual void MapToJson(T entity, JObject json)
        {
            _mapToJsonAction(entity, json);
        }

        public virtual void MapToEntity(JObject json, T entity)
        {
            _mapToEntityAction(json, entity);
        }
        #endregion
    }

    public static class JsonMapperManagerExtensions
    {
        #region Public Methods

        public static JsonMapperConfiguration<T> Configure<T>(this JsonMapperManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");

            var configuration = new JsonMapperConfiguration<T>(manager);
            var mapper = new JsonMapper<T>(configuration);
            manager.Configure(mapper);

            return configuration;
        }
        #endregion
    }

    public class JsonMapperException : Exception
    {
        #region Constructor

        public JsonMapperException()
        {
        }

        public JsonMapperException(string message)
            : base(message)
        {
        }
        #endregion
    }
}