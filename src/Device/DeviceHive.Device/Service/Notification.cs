﻿using System;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a DeviceHive notification.
    /// Notifications are sent by devices to clients.
    /// </summary>
    public class Notification
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets notification identifier (server-assigned).
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets notification timestamp (server-assigned).
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets associated device identifier (server-assigned).
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets notification name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets notification parameters.
        /// </summary>
        public JToken Parameters { get; set; }

        #endregion

        #region Constructor
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Notification()
        {
        }

        /// <summary>
        /// Initializes all notification properties.
        /// </summary>
        /// <param name="name">Notification name.</param>
        /// <param name="parameters">Notification parameters.</param>
        public Notification(string name, JToken parameters)
        {
            Name = name;
            Parameters = parameters;
        }
        #endregion
    }
}
