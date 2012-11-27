using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DeviceHive.Client
{
    /// <summary>
    /// DeviceCommand structure.
    /// </summary>
    /// <remarks>
    /// This class is to describe a command sent to the device. The command can contain a list of parameters in "Name - Value" format.
    /// </remarks>
    public class DeviceCommand
    {
        #region Public Properties

        /// <summary>
        /// This is the command name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// This is the list of command parameters in "Name - Value" format.
        /// </summary>
        public Dictionary<string, string> Parameters { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="name">Initializes Name property</param>
        /// <param name="parameters">Initializes Parameters property</param>
        public DeviceCommand(string name, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");

            Name = name;
            Parameters = parameters ?? new Dictionary<string, string>();
        }
        #endregion
    }
}
