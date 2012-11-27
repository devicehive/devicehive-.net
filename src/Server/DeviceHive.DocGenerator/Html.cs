using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using DeviceHive.API.Models;

namespace DeviceHive.DocGenerator
{
    public static class Html
    {
        public static string Resource(MetadataResource resource)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<pre>");
            builder.AppendLine("{");
            ResourceBlock(builder, resource.Properties, 1);
            builder.AppendLine("}");
            builder.AppendLine("</pre>");
            return builder.ToString();
        }

        private static void ResourceBlock(StringBuilder builder, MetadataParameter[] parameters, int indent)
        {
            var isFirstProperty = true;
            var processed = new List<MetadataParameter>();
            foreach (var parameter in parameters)
            {
                if (processed.Contains(parameter))
                    continue;

                if (!isFirstProperty)
                    builder.AppendLine(",");
                isFirstProperty = false;

                var innerObject = parameters.Where(p => p.Name.StartsWith(parameter.Name + "."));
                if (innerObject.Any())
                {
                    var innerParams = innerObject.Select(o => new MetadataParameter { Name = o.Name.Substring(parameter.Name.Length + 1), Type = o.Type }).ToArray();
                    builder.AppendFormat("{0}<span class=\"green\">&quot;{1}&quot;</span>: {{\n", new string(' ', 4 * indent), Encode(parameter.Name));
                    ResourceBlock(builder, innerParams, indent + 1);
                    builder.AppendFormat("{0}}}", new string(' ', 4 * indent));
                    processed.AddRange(innerObject);
                    continue;
                }

                var innerArray = parameters.Where(p => p.Name.StartsWith(parameter.Name + "[]."));
                if (innerArray.Any())
                {
                    var innerParams = innerArray.Select(o => new MetadataParameter { Name = o.Name.Substring(parameter.Name.Length + 3), Type = o.Type }).ToArray();
                    builder.AppendFormat("{0}<span class=\"green\">&quot;{1}&quot;</span>: [\n", new string(' ', 4 * indent), Encode(parameter.Name));
                    builder.AppendFormat("{0}{{\n", new string(' ', 4 * (indent + 1)));
                    ResourceBlock(builder, innerParams, indent + 2);
                    builder.AppendFormat("{0}}}\n", new string(' ', 4 * (indent + 1)));
                    builder.AppendFormat("{0}]", new string(' ', 4 * indent));
                    processed.AddRange(innerArray);
                    continue;
                }
                
                builder.AppendFormat("{0}<span class=\"green\">&quot;{1}&quot;</span>: <span class=\"blue\">{{{2}}}</span>",
                    new string(' ', 4 * indent), Encode(parameter.Name), Encode(parameter.Type));
            }
            builder.AppendLine();
        }

        public static string Documentation(string text)
        {
            return DocumentationBlock(text);
        }

        private static string DocumentationBlock(string block)
        {
            if (block == null)
                return null;

            // look for content tags
            var startTagMatch = Regex.Match(block, @"\<(\w+)[^\>]*\>");
            if (startTagMatch.Success)
            {
                var tagName = startTagMatch.Groups[1].Value;
                var endTagMatch = Regex.Match(block.Substring(startTagMatch.Index + startTagMatch.Length), @"\<\/" + Regex.Escape(tagName) + @"\>");
                if (endTagMatch.Success)
                {
                    var contents = DocumentationBlock(block.Substring(startTagMatch.Index + startTagMatch.Length, endTagMatch.Index));
                    var substitution = Encode(startTagMatch.Value) + contents + Encode(endTagMatch.Value);
                    switch (tagName)
                    {
                        case "para":
                            substitution = "<p class=\"doc\">" + contents + "</p>";
                            break;
                        case "c":
                        case "code":
                            substitution = "<pre class=\"doc\">" + contents + "</pre>";
                            break;
                        case "list":
                            substitution = "<ul class=\"doc\">" + contents + "</ul>";
                            break;
                        case "item":
                            substitution = "<li class=\"doc\">" + contents + "</li>";
                            break;
                        case "description":
                            substitution = contents;
                            break;
                    }
                    return Encode(block.Substring(0, startTagMatch.Index)) + substitution +
                        DocumentationBlock(block.Substring(startTagMatch.Index + startTagMatch.Length + endTagMatch.Index + endTagMatch.Length));
                }
            }

            // look for self-closing tags
            var tagMatch = Regex.Match(block, @"\<[^\>]+\/\>");
            if (tagMatch.Success)
            {
                var tag = XElement.Parse(tagMatch.Value);
                var substitution = Encode(tagMatch.Value);
                switch (tag.Name.LocalName)
                {
                    case "see":
                        substitution = string.Format(@"<a href=""#Reference/{0}"">{0}</a>", (string)tag.Attribute("cref"));
                        break;

                }
                return Encode(block.Substring(0, tagMatch.Index)) + substitution +
                    DocumentationBlock(block.Substring(tagMatch.Index + tagMatch.Length));
            }

            return Encode(block);
        }

        public static string Encode(string html)
        {
            return html == null ? null : HttpUtility.HtmlEncode(html);
        }
    }
}