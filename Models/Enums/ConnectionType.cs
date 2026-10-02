namespace NexusServiceMarketingSystem.Models.Enums
{
    /// <summary>
    /// The three kinds of service Nexus provides. The single-letter code of each value
    /// (D / B / T) is the first character of every order number and every account ID.
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>Dial-up internet connection (code 'D').</summary>
        DialUp = 1,

        /// <summary>Broadband internet connection (code 'B').</summary>
        Broadband = 2,

        /// <summary>Telephone (landline) only connection (code 'T').</summary>
        Telephone = 3
    }

    /// <summary>
    /// Conversions between <see cref="ConnectionType"/> and its one-letter identifier code.
    /// </summary>
    public static class ConnectionTypeExtensions
    {
        /// <summary>Returns the identifier letter for the given connection type.</summary>
        public static char ToCode(this ConnectionType type) => type switch
        {
            ConnectionType.DialUp => 'D',
            ConnectionType.Broadband => 'B',
            ConnectionType.Telephone => 'T',
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown connection type.")
        };

        /// <summary>Returns the connection type for an identifier letter; throws if the letter is not D, B or T.</summary>
        public static ConnectionType FromCode(char code) => code switch
        {
            'D' => ConnectionType.DialUp,
            'B' => ConnectionType.Broadband,
            'T' => ConnectionType.Telephone,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Connection type code must be 'D', 'B' or 'T'.")
        };

        /// <summary>Non-throwing version of <see cref="FromCode(char)"/>.</summary>
        public static bool TryFromCode(char code, out ConnectionType type)
        {
            switch (code)
            {
                case 'D': type = ConnectionType.DialUp; return true;
                case 'B': type = ConnectionType.Broadband; return true;
                case 'T': type = ConnectionType.Telephone; return true;
                default: type = default; return false;
            }
        }
    }
}
