using System;
using System.Linq;

namespace DeviceHive.Core
{
    /// <summary>
    /// Represents an IP subnet.
    /// Allows to check if an IP address belongs to the current subnet.
    /// </summary>
    public class Subnet
    {
        #region Public Properties

        /// <summary>
        /// Gets subnet IP address.
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
        /// <param name="mask">Subnet mask in number of bits.</param>
        public Subnet(int address, int mask)
        {
            Address = address;
            Mask = mask;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Parses IP address into the integer value.
        /// </summary>
        /// <param name="address">IP address to parse.</param>
        /// <returns>An integer value representing passed IP address.</returns>
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
        /// Parses IP subnet into the Subnet object.
        /// Subnet format: 12.12.12.0/24, or 45.45.45.45
        /// </summary>
        /// <param name="subnet">Subnet string to parse</param>
        /// <returns>Subnet object.</returns>
        public static Subnet ParseSubnet(string subnet)
        {
            var parts = subnet.Split('/');
            var mask = parts.Length > 1 ? int.Parse(parts[1]) : 32;
            return new Subnet(ParseAddress(parts[0]), mask != 0 ? -1 << (32 - mask) : 0);
        }

        /// <summary>
        /// Checks if passed IP address is within the current subnet.
        /// Use <see cref="ParseAddress"/> method to parse string representation of the IP address into the integer value.
        /// </summary>
        /// <param name="address">Integer representation of the IP address to check.</param>
        /// <returns>True if passed IP address is within the current subnet.</returns>
        public bool Includes(int address)
        {
            return (address & Mask) == Address;
        }
        #endregion
    }
}
