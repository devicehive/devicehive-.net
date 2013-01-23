using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DeviceHive.DocGenerator
{
    public class XmlCommentReader
    {
        private XDocument _xml;
        private static Regex nullableTypeNameRegex = new Regex(@"(.*\.Nullable)" + Regex.Escape("`1[[") + "([^,]*),.*");

        #region Constructor

        public XmlCommentReader(string fileName)
        {
            using (var reader = new StreamReader(fileName))
            {
                _xml = XDocument.Load(reader);
            }
        }
        #endregion

        #region Public Methods

        public XElement GetTypeElement(Type type)
        {
            return _xml.XPathSelectElement(string.Format("/doc/members/member[@name='T:{0}']", type.FullName));
        }

        public XElement GetPropertyElement(PropertyInfo property)
        {
            return _xml.XPathSelectElement(string.Format("/doc/members/member[@name='P:{0}.{1}']", property.DeclaringType.FullName, property.Name));
        }

        public XElement GetMethodElement(MethodInfo method)
        {
            return _xml.XPathSelectElement(string.Format("/doc/members/member[@name='M:{0}']", GetMethodName(method)));
        }

        public XElement GetMethodParameterElement(MethodInfo method, string parameterName)
        {
            var methodElement = GetMethodElement(method);
            if (methodElement == null)
                return null;

            return methodElement.XPathSelectElement(string.Format("param[@name='{0}']", parameterName));
        }

        public XElement GetMethodReturnsElement(MethodInfo method)
        {
            var methodElement = GetMethodElement(method);
            if (methodElement == null)
                return null;

            return methodElement.XPathSelectElement("returns");
        }
        #endregion

        #region Private Methods

        private static string GetMethodName(MethodInfo method)
        {
            var name = string.Format("{0}.{1}", method.DeclaringType.FullName, method.Name);
            var parameters = method.GetParameters();
            if (parameters.Length != 0)
            {
                var parameterTypeNames = parameters.Select(p => ProcessTypeName(p.ParameterType.FullName)).ToArray();
                name += string.Format("({0})", string.Join(",", parameterTypeNames));
            }
            return name;
        }

        private static string ProcessTypeName(string typeName)
        {
            var result = nullableTypeNameRegex.Match(typeName);
            if (result.Success)
            {
                return string.Format("{0}{{{1}}}", result.Groups[1].Value, result.Groups[2].Value);
            }
            return typeName;
        }
        #endregion
    }

    public static class XmlCommentExtensions
    {
        #region Public Methods

        public static string ElementContents(this XElement element, string name)
        {
            if (element == null)
                return null;

            return element.Element(name).Contents();
        }

        public static string Contents(this XElement element)
        {
            if (element == null)
                return null;

            // fix cref link in see elements
            foreach (var see in element.XPathSelectElements("//see[@cref]"))
            {
                var cref = see.Attribute("cref");
                cref.Value = cref.Value.Substring(cref.Value.LastIndexOf(".") + 1);
            }

            return string.Concat(element.Nodes()).Trim();
        }
        #endregion
    }
}