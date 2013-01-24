using System;

namespace DeviceHive.DocGenerator
{
    public class Metadata
    {
        public MetadataResource[] Resources { get; set; }
        public MetadataService[] Services { get; set; }
    }

    public class MetadataResource
    {
        public string Name { get; set; }
        public string Documentation { get; set; }
        public MetadataParameter[] Properties { get; set; }
        public MetadataMethod[] Methods { get; set; }
    }

    public class MetadataService
    {
        public string Name { get; set; }
        public string Documentation { get; set; }
        public string Uri { get; set; }
        public MetadataMethod[] Methods { get; set; }
    }

    public class MetadataMethod
    {
        public string Name { get; set; }
        public string Documentation { get; set; }
        public string Verb { get; set; }
        public string Uri { get; set; }
        public MetadataParameter[] UriParameters { get; set; }
        public string Originator { get; set; }
        public string Authorization { get; set; }
        public string RequestDocumentation { get; set; }
        public MetadataParameter[] RequestParameters { get; set; }
        public string ResponseDocumentation { get; set; }
        public MetadataParameter[] ResponseParameters { get; set; }

        public string ID()
        {
            return Name.Replace("/", "");
        }

        public string UriNoQuery()
        {
            if (Uri == null || Uri.IndexOf('?') < 0)
                return Uri;

            return Uri.Substring(0, Uri.IndexOf('?'));
        }
    }

    public class MetadataParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Documentation { get; set; }
        public bool IsRequred { get; set; }
        
        public MetadataParameter()
        {
        }

        public MetadataParameter(string name, string type, string documentation, bool isRequired)
        {
            Name = name;
            Type = type;
            Documentation = documentation;
            IsRequred = isRequired;
        }
    }
}