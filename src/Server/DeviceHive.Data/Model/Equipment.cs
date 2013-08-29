using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Data.Validation;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents an equipment which is installed on devices.
    /// </summary>
    public class Equipment
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public Equipment()
        {
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="name">Equipment name</param>
        /// <param name="code">Equipment code</param>
        /// <param name="type">Equipment type</param>
        public Equipment(string name, string code, string type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Code is null or empty!", "code");
            if (string.IsNullOrEmpty(type))
                throw new ArgumentException("Type is null or empty!", "type");

            this.Name = name;
            this.Code = code;
            this.Type = type;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Equipment identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Associated device class identifier.
        /// </summary>
        public int DeviceClassID { get; set; }

        /// <summary>
        /// Equipment display name.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Equipment code.
        /// It's used to reference particular equipment and it should be unique within a device class.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Code { get; set; }

        /// <summary>
        /// Equipment type.
        /// An arbitrary string representing equipment capabilities.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Type { get; set; }

        /// <summary>
        /// Equipment data, a JSON object with an arbitrary structure.
        /// </summary>
        [JsonField]
        public string Data { get; set; }
        
        #endregion
    }
}
