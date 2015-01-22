using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Validation;
using Newtonsoft.Json.Linq;

namespace DeviceHive.DocGenerator
{
    internal class GeneratorHelper
    {
        private JsonMapperManager _jsonMapperManager;
        private XmlCommentReader _xmlCommentReader;

        public GeneratorHelper(JsonMapperManager jsonMapperManager, XmlCommentReader xmlCommentReader)
        {
            _jsonMapperManager = jsonMapperManager;
            _xmlCommentReader = xmlCommentReader;
        }

        public MetadataParameter[] GetTypeParameters(Type type, JsonMapperEntryMode? mode = null, bool patch = false, string prefix = null)
        {
            // get JSON mapping manager
            var mapper = _jsonMapperManager.GetMapper(type);
            if (mapper == null)
                return new MetadataParameter[] { };

            // create parameters from mapping entries
            var parameters = new List<MetadataParameter>();
            foreach (var parameter in mapper.Entries.Where(e => mode == null || (mode.Value & e.Mode) == mode.Value))
            {
                // add parameter that corresponds to the mapped property
                var propertyType = parameter.EntityProperty.PropertyType;
                var isJsonObject = parameter.EntityProperty.IsDefined(typeof(JsonFieldAttribute), false);
                var param = new MetadataParameter
                {
                    Name = prefix + parameter.JsonProperty,
                    Type = isJsonObject ? "object" : ToJsonType(propertyType),
                    IsRequred = !patch && IsRequired(parameter.EntityProperty),
                    Documentation = _xmlCommentReader.GetPropertyElement(parameter.EntityProperty).ElementContents("summary"),
                };
                parameters.Add(param);

                // add child object parameters
                if (param.Type == "object" && !isJsonObject)
                {
                    parameters.AddRange(GetTypeParameters(propertyType, mode, false, param.Name + "."));
                }
                else if (param.Type == "array")
                {
                    var elementType = propertyType.GetInterfaces().First(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IEnumerable<>)).GetGenericArguments().First();
                    if (ToJsonType(elementType) == "object")
                    {
                        parameters.AddRange(GetTypeParameters(elementType, mode, false, param.Name + "[]."));
                    }
                }
            }
            return parameters.ToArray();
        }

        public void AdjustParameters(List<MetadataParameter> parameters, XElement adjustElement, JsonMapperEntryMode? jsonMode = null)
        {
            foreach (var parameterElement in adjustElement.Elements("parameter")
                .Where(p => p.Attribute("name") != null))
            {
                var name = (string)parameterElement.Attribute("name");
                var type = (string)parameterElement.Attribute("type");
                var mode = (string)parameterElement.Attribute("mode");
                var required = (bool?)parameterElement.Attribute("required");
                var after = (string)parameterElement.Attribute("after");

                // remove an existing parameter
                if (mode == "remove")
                {
                    parameters.RemoveAll(p => p.Name.StartsWith(name));
                    continue;
                }

                // add or update an existing parameter
                var param = parameters.FirstOrDefault(p => p.Name == name);
                if (param == null)
                {
                    param = new MetadataParameter { Name = name, Type = type };

                    var index = parameters.Count;
                    if (after != null)
                    {
                        var target = parameters.FirstOrDefault(p => p.Name == after);
                        if (target != null)
                            index = parameters.IndexOf(target) + 1;
                    }
                    parameters.Insert(index, param);
                }
                if (!string.IsNullOrEmpty(parameterElement.Contents()))
                {
                    param.Documentation = parameterElement.Contents();
                }
                if (required != null)
                {
                    param.IsRequred = required.Value;
                }

                // if element includes cref - parse the specified type and add parameters from it
                var cref = GetCrefType(parameterElement);
                if (cref != null)
                {
                    if (param.Type == null)
                        param.Type = "object";
                    var paramJsonMode = jsonMode;
                    if (paramJsonMode != null && mode == "OneWayOnly")
                        paramJsonMode = paramJsonMode.Value | JsonMapperEntryMode.OneWayOnly;
                    parameters.AddRange(GetTypeParameters(cref, paramJsonMode, prefix: param.Name + (type == "array" ? "[]" : null) + "."));
                }
            }
        }

        public Type GetCrefType(XElement element)
        {
            // parses cref attribute and returns corresponding type
            var typeName = (string)element.Attribute("cref");
            if (typeName == null || !typeName.StartsWith("T:"))
                return null;

            var type = Type.GetType(typeName.Substring(2), false);
            if (type == null)
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(typeName.Substring(2)))
                    .Where(t => t != null).FirstOrDefault();
            }

            return type;
        }

        public bool IsRequired(PropertyInfo property)
        {
            if (property.IsDefined(typeof(RequiredAttribute), true))
                return true;

            if (property.IsDefined(typeof(DefaultValueAttribute), true))
                return false;

            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return false;

            return property.PropertyType.IsValueType;
        }

        public string ToJsonType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments().First();

            if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) && type != typeof(string) && type != typeof(JObject))
                return "array";

            if (type.IsEnum)
                return "string";

            if (type == typeof(Guid))
                return "guid";

            if (Type.GetTypeCode(type) == TypeCode.Object)
                return "object";

            if (Type.GetTypeCode(type).ToString().StartsWith("Int") || Type.GetTypeCode(type).ToString().StartsWith("UInt"))
                return "integer";

            return type.Name.ToLower();
        }
    }
}
