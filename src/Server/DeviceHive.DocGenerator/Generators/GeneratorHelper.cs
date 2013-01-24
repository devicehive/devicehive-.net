using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Validation;

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

        public MetadataParameter[] GetTypeParameters(Type type, JsonMapperEntryMode? exclude = null, string prefix = null)
        {
            // get JSON mapping manager
            var mapper = _jsonMapperManager.GetMapper(type);
            if (mapper == null)
                return new MetadataParameter[] { };

            // create parameters from mapping entries
            var parameters = new List<MetadataParameter>();
            foreach (var parameter in mapper.Entries.Where(e => exclude == null || e.Mode != exclude.Value))
            {
                // add parameter that corresponds to the mapped property
                var isJsonObject = parameter.EntityProperty.IsDefined(typeof(JsonFieldAttribute), false);
                var param = new MetadataParameter
                {
                    Name = (prefix == null ? null : prefix + ".") + parameter.JsonProperty,
                    Type = isJsonObject ? "object" : ToJsonType(parameter.EntityProperty.PropertyType),
                    IsRequred = IsRequired(parameter.EntityProperty),
                    Documentation = _xmlCommentReader.GetPropertyElement(parameter.EntityProperty).ElementContents("summary"),
                };
                parameters.Add(param);

                // add child object parameters
                if (param.Type == "object" && !isJsonObject)
                {
                    parameters.AddRange(GetTypeParameters(parameter.EntityProperty.PropertyType, exclude, param.Name));
                }
            }
            return parameters.ToArray();
        }

        public void AdjustParameters(List<MetadataParameter> parameters, XElement adjustElement, JsonMapperEntryMode? exclude = null)
        {
            foreach (var parameterElement in adjustElement.Elements("parameter")
                .Where(p => p.Attribute("name") != null))
            {
                var name = (string)parameterElement.Attribute("name");
                var type = (string)parameterElement.Attribute("type");
                var mode = (string)parameterElement.Attribute("mode");
                var required = (bool?)parameterElement.Attribute("required");

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
                    parameters.Add(param);
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
                    parameters.AddRange(GetTypeParameters(cref, exclude, param.Name + (type == "array" ? "[]" : null)));
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
