using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// Specifies parameter names on properties of strongly-typed notification and command objects.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ParameterAttribute : Attribute
    {
        #region Public Properties

        /// <summary>
        /// Gets parameter name.
        /// </summary>
        public string Name { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        public ParameterAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty", "name");

            Name = name;
        }
        #endregion
    }
}
