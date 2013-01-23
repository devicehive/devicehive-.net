namespace DeviceHive.Binary
{
    /// <summary>
    /// Data types that can be used in DeviceHive binary protocol
    /// </summary>
    public enum DataType : byte
    {
        /// <summary>
        /// Empty data
        /// </summary>
        Null = 0,

        /// <summary>
        /// Unsigned byte value
        /// </summary>
        Byte = 1,

        /// <summary>
        /// Unsigned word value
        /// </summary>
        Word = 2,

        /// <summary>
        /// Unsigned double word value
        /// </summary>
        Dword = 3,

        /// <summary>
        /// Unsigned quad word value
        /// </summary>
        Qword = 4,

        /// <summary>
        /// Signed byte
        /// </summary>
        SignedByte = 5,

        /// <summary>
        /// Signed word value
        /// </summary>
        SignedWord = 6,

        /// <summary>
        /// Signed double word value
        /// </summary>
        SignedDword = 7,

        /// <summary>
        /// Signed quad word value
        /// </summary>
        SignedQword = 8,

        /// <summary>
        /// Floating point value
        /// </summary>
        Single = 9,

        /// <summary>
        /// Floating point value (double precession)
        /// </summary>
        Double = 10,

        /// <summary>
        /// Boolean value
        /// </summary>
        Boolean = 11,

        /// <summary>
        /// GUID value
        /// </summary>
        Guid = 12,

        /// <summary>
        /// UTF-8 string value
        /// </summary>
        UtfString = 13,

        /// <summary>
        /// Binary (byte array) value
        /// </summary>
        Binary = 14,

        /// <summary>
        /// Array of custom data
        /// </summary>
        Array = 15,

        /// <summary>
        /// Object with custom data
        /// </summary>
        Object = 16,
    }
}