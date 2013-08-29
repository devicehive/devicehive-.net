using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Xml.XPath;
using DeviceHive.API;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Data;
using Ninject;

namespace DeviceHive.DocGenerator
{
    public class MetadataGenerator
    {
        private XmlCommentReader _dataXmlCommentReader;
        private XmlCommentReader _apiXmlCommentReader;
        private GeneratorHelper _helper;

        public MetadataGenerator()
        {
            var kernel = new StandardKernel();
            kernel.Bind<JsonMapperManager>().ToSelf().InSingletonScope().OnActivation(JsonMapperConfig.ConfigureMapping);
            kernel.Bind<DataContext>().ToSelf().InSingletonScope()
                .OnActivation<DataContext>(context => { context.SetRepositoryCreator(type => kernel.Get(type)); });

            _dataXmlCommentReader = new XmlCommentReader("DeviceHive.Data.xml");
            _apiXmlCommentReader = new XmlCommentReader("DeviceHive.API.xml");

            _helper = new GeneratorHelper(kernel.Get<JsonMapperManager>(), _dataXmlCommentReader);
        }

        public Metadata Generate()
        {
            var httpConfiguration = new HttpConfiguration();
            RouteConfig.RegisterRoutes(httpConfiguration.Routes);

            var apiExplorer = httpConfiguration.Services.GetApiExplorer();
            var metadata = new Metadata
            {
                Resources = apiExplorer.ApiDescriptions
                    .OrderBy(d => d.ActionDescriptor.ControllerDescriptor.ControllerName)
                    .Select(d => new { Resource = GetResourceType(d), Method = d })
                    .Where(d => d.Resource != null)
                    .GroupBy(d => d.Resource, d => d.Method)
                    .Select(g => new
                    {
                        Resource = g.Key,
                        Methods = g.GroupBy(m =>
                            GetMethodName((ReflectedHttpActionDescriptor)m.ActionDescriptor)).Select(m => m.First()).ToList(),
                    })
                    .Select(cd => new MetadataResource
                    {
                        Name = cd.Resource == null ? null : cd.Resource.Name,
                        Documentation = GetTypeDocumentation(cd.Resource),
                        Properties = cd.Resource == null ? null : _helper.GetTypeParameters(cd.Resource),
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
            var typeElement = _apiXmlCommentReader.GetTypeElement(descriptor.MethodInfo.DeclaringType);
            if (typeElement == null)
                return null;

            var resourceElement = typeElement.Element("resource");
            if (resourceElement == null)
                return null;

            return _helper.GetCrefType(resourceElement);
        }

        private string GetTypeDocumentation(Type type)
        {
            if (type == null)
                return null;

            // get XML documentation for specified resource type
            var resourceTypeElement = _dataXmlCommentReader.GetTypeElement(type);
            return resourceTypeElement.ElementContents("summary");
        }

        private string GetMethodName(ReflectedHttpActionDescriptor descriptor)
        {
            // get XML name parameter for corresponding method
            var methodElement = _apiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            return methodElement.ElementContents("name");
        }

        private string GetMethodDocumentation(ReflectedHttpActionDescriptor descriptor)
        {
            // get XML documentation for corresponding method
            var methodElement = _apiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            return methodElement.ElementContents("summary");
        }

        private MetadataParameter[] GetUrlParameters(ApiDescription method)
        {
            // get list of URL parameters for specified method
            var descriptor = method.ActionDescriptor as ReflectedHttpActionDescriptor;
            var parameters = method.ParameterDescriptions
                .Where(p => p.Source == ApiParameterSource.FromUri)
                .Select(p =>
                {
                    var parameterElement = _apiXmlCommentReader.GetMethodParameterElement(descriptor.MethodInfo, p.Name);
                    return new MetadataParameter
                    {
                        Name = p.Name,
                        Type = _helper.ToJsonType(p.ParameterDescriptor.ParameterType),
                        Documentation = parameterElement.Contents(),
                        IsRequred = !p.ParameterDescriptor.IsOptional &&
                            !(p.ParameterDescriptor.ParameterType.IsGenericType &&
                            p.ParameterDescriptor.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>)),
                    };
                }).ToList();

            // adjust URL parameters from the XML query element
            var methodElement = _apiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            if (methodElement != null)
            {
                var queryElement = methodElement.Element("query");
                if (queryElement != null)
                {
                    // read cref type on the XML query element
                    var resourceType = _helper.GetCrefType(queryElement);
                    if (resourceType != null)
                    {
                        parameters.AddRange(_helper.GetTypeParameters(resourceType, JsonMapperEntryMode.FromJson));
                    }

                    // adjust URL parameters according to the XML query element
                    _helper.AdjustParameters(parameters, queryElement, JsonMapperEntryMode.FromJson);
                }
            }

            return parameters.ToArray();
        }

        private string GetAuthorization(HttpActionDescriptor description)
        {
            var filters = description.GetFilters().Union(description.ControllerDescriptor.GetFilters()).ToList();
            var authorizeAdmin = filters.OfType<AuthorizeAdminAttribute>().FirstOrDefault();
            var authorizeUser = filters.OfType<AuthorizeUserAttribute>().FirstOrDefault();
            var authorizeUserOrDevice = filters.OfType<AuthorizeUserOrDeviceAttribute>().FirstOrDefault();

            if (authorizeAdmin != null)
                return "Administrator";

            if (authorizeUser != null)
                return "User" + (authorizeUser.AccessKeyAction == null ? null : " or Key (" + authorizeUser.AccessKeyAction + ")");

            if (authorizeUserOrDevice != null)
                return "User or Device" + (authorizeUserOrDevice.AccessKeyAction == null ? null : " or Key (" + authorizeUserOrDevice.AccessKeyAction + ")");

            return "None";
        }

        private string GetRequestDocumentation(ReflectedHttpActionDescriptor descriptor)
        {
            // get XML documentation for 'json' parameter element
            return _apiXmlCommentReader.GetMethodParameterElement(descriptor.MethodInfo, "json").Contents()
                ?? "Do not supply a request body with this method.";
        }

        private MetadataParameter[] GetRequestParameters(ReflectedHttpActionDescriptor descriptor)
        {
            var methodElement = _apiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            if (methodElement == null)
                return null;

            var parameters = new List<MetadataParameter>();

            // read cref type on the 'json' parameter element
            var methodParamElement = methodElement.XPathSelectElement("param[@name='json']");
            if (methodParamElement != null)
            {
                var resourceType = _helper.GetCrefType(methodParamElement);
                if (resourceType != null)
                {
                    parameters.AddRange(_helper.GetTypeParameters(resourceType, JsonMapperEntryMode.FromJson));
                }
            }

            // adjust parameters according to the XML request element
            var requestElement = methodElement.Element("request");
            if (requestElement != null)
            {
                _helper.AdjustParameters(parameters, requestElement, JsonMapperEntryMode.FromJson);
            }

            return parameters.ToArray();
        }

        private string GetResponseDocumentation(ReflectedHttpActionDescriptor descriptor)
        {
            // get XML documentation for returns element
            return _apiXmlCommentReader.GetMethodReturnsElement(descriptor.MethodInfo).Contents()
                ?? "If successful, this method returns an empty response body.";
        }

        private MetadataParameter[] GetResponseParameters(ReflectedHttpActionDescriptor descriptor)
        {
            var methodElement = _apiXmlCommentReader.GetMethodElement(descriptor.MethodInfo);
            if (methodElement == null)
                return null;

            var parameters = new List<MetadataParameter>();

            // read cref type on the XML returns element
            var methodReturnsElement = methodElement.Element("returns");
            if (methodReturnsElement != null)
            {
                var resourceType = _helper.GetCrefType(methodReturnsElement);
                if (resourceType != null)
                {
                    var oneWayOnly = (string)methodReturnsElement.Attribute("mode") == "OneWayOnly";
                    parameters.AddRange(_helper.GetTypeParameters(resourceType, JsonMapperEntryMode.ToJson | (oneWayOnly ? JsonMapperEntryMode.OneWayOnly : 0)));
                }
            }

            // adjust parameters according to the XML response element
            var responseElement = methodElement.Element("response");
            if (responseElement != null)
            {
                _helper.AdjustParameters(parameters, responseElement, JsonMapperEntryMode.ToJson);
            }

            return parameters.ToArray();
        }
    }
}
