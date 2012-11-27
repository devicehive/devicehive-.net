using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Xml.Linq;
using System.Xml.XPath;
using DeviceHive.API.Business;
using DeviceHive.API.Filters;
using DeviceHive.API.Mapping;
using DeviceHive.API.Models;
using DeviceHive.Data.Validation;
using Ninject;

namespace DeviceHive.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MetadataController : BaseController
    {
        private XmlCommentReader DataXmlCommentReader { get; set; }
        private XmlCommentReader ApiXmlCommentReader { get; set; }

        public MetadataController([Named("Data")] XmlCommentReader dataXmlCommentReader, [Named("API")] XmlCommentReader apiXmlCommentReader)
        {
            DataXmlCommentReader = dataXmlCommentReader;
            ApiXmlCommentReader = apiXmlCommentReader;
        }

        public Metadata Get()
        {
            var apiExplorer = GlobalConfiguration.Configuration.Services.GetApiExplorer();
            var metadata = new Metadata
                {
                    Resources = apiExplorer.ApiDescriptions
                        .OrderBy(d => d.ActionDescriptor.ControllerDescriptor.ControllerName)
                        .Select(d => new { Resource = GetResourceType(d), Method = d })
                        .Where(d => d.Resource != null)
                        .GroupBy(d => d.Resource, d => d.Method)
                        .Select(g => new { Resource = g.Key, Methods = g.GroupBy(m =>
                            GetMethodName((ReflectedHttpActionDescriptor)m.ActionDescriptor)).Select(m => m.First()).ToList(), })
                        .Select(cd => new MetadataResource
                            {
                                Name = cd.Resource == null ? null : cd.Resource.Name,
                                Documentation = GetTypeDocumentation(cd.Resource),
                                Properties = cd.Resource == null ? null : GetTypeParameters(cd.Resource),
                                Methods = cd.Methods
                                    .Where(m => GetMethodName((ReflectedHttpActionDescriptor)m.ActionDescriptor) != null)
                                    .Select(m => new MetadataMethod
                                        {
                                            Name = GetMethodName((ReflectedHttpActionDescriptor)m.ActionDescriptor),
                                            Documentation = GetMethodDocumentation((ReflectedHttpActionDescriptor)m.ActionDescriptor),
                                            Verb = m.HttpMethod.Method,
                                            Uri = "/" + m.RelativePath,
                                            UriParameters = GetUrlParameters(m),
                                            Authorization = GetAuthorization(m.ActionDescriptor),
                                            RequestDocumentation = GetRequestDocumentation((ReflectedHttpActionDescriptor)m.ActionDescriptor),
                                            RequestParameters = GetRequestParameters((ReflectedHttpActionDescriptor)m.ActionDescriptor),
                                            ResponseDocumentation = GetResponseDocumentation((ReflectedHttpActionDescriptor)m.ActionDescriptor),
                                            ResponseParameters = GetResponseParameters((ReflectedHttpActionDescriptor)m.ActionDescriptor),
                                        }).ToArray(),
                            }).ToArray(),
                };


            return metadata;
        }

        private Type GetResourceType(ApiDescription description)
        {
            var descriptor = description.ActionDescriptor as ReflectedHttpActionDescriptor;

            // read associated resource type (resource XML element on controller type)
            var typeElement = ApiXmlCommentReader.GetTypeElement(descriptor.MethodInfo.DeclaringType);
            if (typeElement == null)
                return null;

            var resourceElement = typeElement.Element("resource");
            if (resourceElement == null)
                return null;

            return GetCrefType(resourceElement);
        }

        private string GetTypeDocumentation(Type type)
        {
            if (type == null)
                return null;

            // get XML documentation for specified resource type
            var resourceTypeElement = DataXmlCommentReader.GetTypeElement(type);
            return resourceTypeElement.ElementContents("summary");
        }

        private string GetMethodName(ReflectedHttpActionDescriptor descriptor)
        {
            // get XML name parameter for corresponding method
            var methodElement = ApiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            return methodElement.ElementContents("name");
        }

        private string GetMethodDocumentation(ReflectedHttpActionDescriptor descriptor)
        {
            // get XML documentation for corresponding method
            var methodElement = ApiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            return methodElement.ElementContents("summary");
        }

        private MetadataParameter[] GetUrlParameters(ApiDescription method)
        {
            // get list of URL parameters for specified method
            var descriptor = method.ActionDescriptor as ReflectedHttpActionDescriptor;
            return method.ParameterDescriptions
                .Where(p => p.Source == ApiParameterSource.FromUri)
                .Select(p =>
                    {
                        var parameterElement = ApiXmlCommentReader.GetMethodParameterElement(descriptor.MethodInfo, p.Name);
                        return new MetadataParameter
                            {
                                Name = p.Name,
                                Type = ToJsonType(p.ParameterDescriptor.ParameterType),
                                Documentation = parameterElement.Contents(),
                                IsRequred = p.ParameterDescriptor.DefaultValue == null &&
                                    !(p.ParameterDescriptor.ParameterType.IsGenericType &&
                                    p.ParameterDescriptor.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>)),
                            };
                    }).ToArray();
        }

        private string GetRequestDocumentation(ReflectedHttpActionDescriptor descriptor)
        {
            // get XML documentation for 'json' parameter element
            return ApiXmlCommentReader.GetMethodParameterElement(descriptor.MethodInfo, "json").Contents()
                ?? "Do not supply a request body with this method.";
        }

        private MetadataParameter[] GetRequestParameters(ReflectedHttpActionDescriptor descriptor)
        {
            var methodElement = ApiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            if (methodElement == null)
                return null;

            var parameters = new List<MetadataParameter>();

            // read cref type on the 'json' parameter element
            var methodParamElement = methodElement.XPathSelectElement("param[@name='json']");
            if (methodParamElement != null)
            {
                var resourceType = GetCrefType(methodParamElement);
                if (resourceType != null)
                {
                    parameters.AddRange(GetTypeParameters(resourceType, JsonMapperEntryMode.OneWay));
                }
            }

            // read XML request element
            var requestElement = methodElement.Element("request");
            if (requestElement != null)
            {
                foreach (var parameterElement in requestElement.Elements("parameter")
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
                        parameters.AddRange(GetTypeParameters(cref, JsonMapperEntryMode.OneWay, param.Name + (type == "array" ? "[]" : null)));
                    }
                }
            }

            return parameters.ToArray();
        }

        private string GetResponseDocumentation(ReflectedHttpActionDescriptor descriptor)
        {
            // get XML documentation for returns element
            return ApiXmlCommentReader.GetMethodReturnsElement(descriptor.MethodInfo).Contents()
                ?? "If successful, this method returns an empty response body.";
        }

        private MetadataParameter[] GetResponseParameters(ReflectedHttpActionDescriptor descriptor)
        {
            var methodElement = ApiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            if (methodElement == null)
                return null;

            var parameters = new List<MetadataParameter>();

            // read cref type on the XML returns element
            var methodReturnsElement = methodElement.Element("returns");
            if (methodReturnsElement != null)
            {
                var resourceType = GetCrefType(methodReturnsElement);
                if (resourceType != null)
                {
                    parameters.AddRange(GetTypeParameters(resourceType, JsonMapperEntryMode.OneWayToSource));
                }
            }

            // read XML response element
            var responseElement = methodElement.Element("response");
            if (responseElement != null)
            {
                foreach (var parameterElement in responseElement.Elements("parameter")
                    .Where(p => p.Attribute("name") != null))
                {
                    var name = (string)parameterElement.Attribute("name");
                    var type = (string)parameterElement.Attribute("type");
                    var mode = (string)parameterElement.Attribute("mode");

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
                    
                    // if element includes cref - parse the specified type and add parameters from it
                    var cref = GetCrefType(parameterElement);
                    if (cref != null)
                    {
                        parameters.AddRange(GetTypeParameters(cref, JsonMapperEntryMode.OneWayToSource, param.Name + (type == "array" ? "[]" : null)));
                    }
                }
            }

            return parameters.ToArray();
        }

        private Type GetCrefType(XElement element)
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

        private MetadataParameter[] GetTypeParameters(Type type, JsonMapperEntryMode? exclude = null, string prefix = null)
        {
            // get JSON mapping manager
            var mapper = JsonMapperManager.GetMapper(type);
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
                        Documentation = DataXmlCommentReader.GetPropertyElement(parameter.EntityProperty).ElementContents("summary"),
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

        private string GetAuthorization(HttpActionDescriptor description)
        {
            var filters = description.GetFilters().Union(description.ControllerDescriptor.GetFilters()).ToList();
            var authorizeUser = filters.OfType<AuthorizeUserAttribute>().FirstOrDefault();
            var authorizeDevice = filters.OfType<AuthorizeDeviceAttribute>().FirstOrDefault();
            var authorizeDeviceOrUser = filters.OfType<AuthorizeDeviceOrUserAttribute>().FirstOrDefault();

            if (authorizeDeviceOrUser != null)
                return string.Format("Device and User ({0})", authorizeDeviceOrUser.Roles ?? "All Roles");

            if (authorizeUser != null)
                return string.Format("User ({0})", authorizeUser.Roles ?? "All Roles");

            if (authorizeDevice != null)
                return "Device";

            return "None";
        }

        private bool IsRequired(PropertyInfo property)
        {
            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return false;
            if (property.PropertyType.IsValueType)
                return true;

            return property.GetCustomAttributes(typeof(RequiredAttribute), true).Any();
        }

        private string ToJsonType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments().First();

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
