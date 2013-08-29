using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a permission of an access key.
    /// Permissions specify requirements to a callee (domain, subnet), a list of allowed methods and accessible networks and/or devices.
    /// </summary>
    public class AccessKeyPermission
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public AccessKeyPermission()
        {
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Access key permission identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Associated access key identifier (used internally by EF repository).
        /// </summary>
        public int AccessKeyID { get; private set; }

        /// <summary>
        /// A collection of domains to which this permission applies.
        /// Only API requests from the specified domains will be authorized with this permission.
        /// Set to null to allow callees from any domain to make specified actions.
        /// </summary>
        public string[] Domains { get; set; }

        /// <summary>
        /// A collection of source IP addresses or subnets to which this permission applies.
        /// Only API requests from the specified addresses/subnets will be authorized with this permission.
        /// Set to null to allow any callees to make specified actions.
        /// Subnet format example: 12.12.12.12, or 12.12.12.0/24
        /// </summary>
        public string[] Subnets { get; set; }

        /// <summary>
        /// A collection of allowed actions.
        /// Available values:
        /// <list type="bullet">
        ///     <item><description>GetNetwork: get information about network</description></item>
        ///     <item><description>GetDevice: get information about device and device class</description></item>
        ///     <item><description>GetDeviceState: get information about current device equipment state</description></item>
        ///     <item><description>GetDeviceNotification: get or subscribe to device notifications</description></item>
        ///     <item><description>GetDeviceCommand: get or subscribe to commands sent to device</description></item>
        ///     <item><description>RegisterDevice: register a device</description></item>
        ///     <item><description>CreateDeviceNotification: post notifications on behalf of device</description></item>
        ///     <item><description>CreateDeviceCommand: post commands to device</description></item>
        ///     <item><description>UpdateDeviceCommand: update status of commands on behalf of device</description></item>
        /// </list>
        /// </summary>
        public string[] Actions { get; set; }

        /// <summary>
        /// A collection of identifiers of allowed networks.
        /// Only API requests for devices within the allowed networks will be authorized with this permission.
        /// Set to null to allow callees to access all networks permitted for the owner user.
        /// </summary>
        public int[] Networks { get; set; }

        /// <summary>
        /// A collection of unique identifiers of allowed devices.
        /// Only API requests for allowed devices will be authorized with this permission.
        /// Set to null to allow callees to access all devices permitted for the owner user.
        /// </summary>
        public string[] Devices { get; set; }

        /// <summary>
        /// A JSON configuration object which holds domains, subnets, actions, networks and devices.
        /// Used for EF serialization only.
        /// </summary>
        [Required]
        public string Configuration
        {
            get
            {
                return new JObject(
                    new JProperty("domains", Domains),
                    new JProperty("subnets", Subnets),
                    new JProperty("actions", Actions),
                    new JProperty("networks", Networks),
                    new JProperty("devices", Devices)
                ).ToString(Formatting.None);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Value is null or empty!");

                var j = JObject.Parse(value);
                Domains = j["domains"].ToObject<string[]>();
                Subnets = j["subnets"].ToObject<string[]>();
                Actions = j["actions"].ToObject<string[]>();
                Networks = j["networks"].ToObject<int[]>();
                Devices = j["devices"].ToObject<string[]>();
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if specified domain is allowed by the current permission.
        /// </summary>
        /// <param name="domain">Domain to check.</param>
        /// <returns>True if passed domain is allowed.</returns>
        public bool IsDomainAllowed(string domain)
        {
            if (string.IsNullOrEmpty(domain))
                throw new ArgumentException("Domain is null or empty!", "domain");

            return Domains == null || Domains.Contains(domain, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if specified address is allowed by the current permission.
        /// </summary>
        /// <param name="address">Address to check.</param>
        /// <returns>True if passed address is allowed.</returns>
        public bool IsAddressAllowed(string address)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Address is null or empty!", "address");

            if (Subnets == null)
                return true;

            var parsedAddress = Subnet.ParseAddress(address);
            return Subnets.Any(s => Subnet.ParseSubnet(s).Includes(parsedAddress));
        }

        /// <summary>
        /// Checks if specified action is allowed by the current permission.
        /// </summary>
        /// <param name="action">Action to check.</param>
        /// <returns>True if passed action is allowed.</returns>
        public bool IsActionAllowed(string action)
        {
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action is null or empty!", "action");

            return Actions == null || Actions.Contains(action, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if specified network is allowed by the current permission.
        /// </summary>
        /// <param name="network">Network to check.</param>
        /// <returns>True if passed network is allowed.</returns>
        public bool IsNetworkAllowed(int network)
        {
            return Networks == null || Networks.Contains(network);
        }

        /// <summary>
        /// Checks if specified device is allowed by the current permission.
        /// </summary>
        /// <param name="device">Device to check.</param>
        /// <returns>True if passed device is allowed.</returns>
        public bool IsDeviceAllowed(string device)
        {
            if (string.IsNullOrEmpty(device))
                throw new ArgumentException("Device is null or empty!", "device");

            return Devices == null || Devices.Contains(device, StringComparer.OrdinalIgnoreCase);
        }
        #endregion
    }

    #region Subnet class

    /// <summary>
    /// Represents a network subnet.
    /// </summary>
    internal class Subnet
    {
        #region Public Properties

        /// <summary>
        /// Gets subnet address.
        /// </summary>
        public int Address { get; private set; }

        /// <summary>
        /// Gets subnet mask.
        /// </summary>
        public int Mask { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="address">Subnet address.</param>
        /// <param name="mask">Subnet mask.</param>
        public Subnet(int address, int mask)
        {
            Address = address;
            Mask = mask;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Parses an IP address.
        /// </summary>
        /// <param name="address">IP address to parse.</param>
        /// <returns>Integer value representing passed IP address.</returns>
        public static int ParseAddress(string address)
        {
            if (string.Equals(address, "::1"))
                address = "127.0.0.1";

            var stringBlocks = address.Split('.');
            if (stringBlocks.Length != 4)
                throw new FormatException("Invalid IP address: " + address);

            var blocks = stringBlocks.Select(s => byte.Parse(s)).ToArray();
            return (blocks[0] << 24) | (blocks[1] << 16) | (blocks[2] << 8) | (blocks[3]);
        }

        /// <summary>
        /// Parses a IP subnet.
        /// </summary>
        /// <param name="subnet">Subnet to parse.</param>
        /// <returns>Subnet object.</returns>
        public static Subnet ParseSubnet(string subnet)
        {
            var parts = subnet.Split('/');
            var mask = parts.Length > 1 ? int.Parse(parts[1]) : 32;
            return new Subnet(ParseAddress(parts[0]), mask != 0 ? -1 << (32 - mask) : 0);
        }

        /// <summary>
        /// Checks if the current subnet includes the passed IP address.
        /// </summary>
        /// <param name="address">IP address, use <see cref="ParseAddress"/> to parse its string presentation.</param>
        /// <returns>True if the current subnet includes the passed IP address.</returns>
        public bool Includes(int address)
        {
            return (address & Mask) == Address;
        }
        #endregion
    }
    #endregion
}
