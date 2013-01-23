using System;

namespace DeviceHive.DocGenerator
{
    /// <summary>
    /// Represents the service metadata
    /// </summary>
    public class Metadata
    {
        #region Public Methods

        /// <summary>
        /// Gets array of resources
        /// </summary>
        public MetadataResource[] Resources { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents the service resource
    /// </summary>
    public class MetadataResource
    {
        #region Public Methods

        /// <summary>
        /// Gets resource name
        /// The value is extracted from controller's 'resource/@cref' attribute
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets resource documentation
        /// The value is extracted from 'summary' element of the resource type
        /// </summary>
        public string Documentation { get; set; }

        /// <summary>
        /// Gets resource properties
        /// </summary>
        public MetadataParameter[] Properties { get; set; }

        /// <summary>
        /// Gets resource methods
        /// </summary>
        public MetadataMethod[] Methods { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents the service method
    /// </summary>
    public class MetadataMethod
    {
        #region Public Methods

        /// <summary>
        /// Gets method name
        /// The value is extracted from action's 'name' element
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets method documentation
        /// The value is extracted from action's 'summary' element
        /// </summary>
        public string Documentation { get; set; }

        /// <summary>
        /// Gets method HTTP verb
        /// </summary>
        public string Verb { get; set; }

        /// <summary>
        /// Gets method URL (with parameter placeholders)
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets URL parameters
        /// </summary>
        public MetadataParameter[] UriParameters { get; set; }

        /// <summary>
        /// Gets method authorization
        /// </summary>
        public string Authorization { get; set; }

        /// <summary>
        /// Gets request documentation
        /// The value is extracted from action's 'json' parameter element
        /// </summary>
        public string RequestDocumentation { get; set; }

        /// <summary>
        /// Gets list of request parameters
        /// The list is extracted from action's 'json/@cref' parameter attribute and/or action's 'request' element
        /// </summary>
        public MetadataParameter[] RequestParameters { get; set; }

        /// <summary>
        /// Gets response documentation
        /// The value is extracted from action's 'returns' element
        /// </summary>
        public string ResponseDocumentation { get; set; }

        /// <summary>
        /// Gets list of response parameters
        /// The list is extracted from action's 'returns/@cref' parameter attribute and/or action's 'response' element
        /// </summary>
        public MetadataParameter[] ResponseParameters { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the method URI without query string
        /// </summary>
        /// <returns>Method URI without query string</returns>
        public string UriNoQuery()
        {
            if (Uri == null || Uri.IndexOf('?') < 0)
                return Uri;

            return Uri.Substring(0, Uri.IndexOf('?'));
        }
        #endregion
    }

    /// <summary>
    /// Represents the service resource/method parameter
    /// </summary>
    public class MetadataParameter
    {
        #region Public Methods

        /// <summary>
        /// Gets parameter name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets parameter type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets parameter documentation
        /// The value is extracted from 'summary' element of the corresponding property
        /// </summary>
        public string Documentation { get; set; }

        /// <summary>
        /// Gets flag indicating if this parameter is required
        /// </summary>
        public bool IsRequred { get; set; }

        #endregion
    }
}