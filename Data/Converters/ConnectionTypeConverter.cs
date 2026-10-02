using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Converters
{
    /// <summary>
    /// Stores <see cref="ConnectionType"/> in the database as its one-letter code
    /// ('D', 'B' or 'T'), the same letter that starts order numbers and account IDs.
    /// </summary>
    public class ConnectionTypeConverter : ValueConverter<ConnectionType, string>
    {
        public ConnectionTypeConverter()
            : base(value => ToProvider(value), value => FromProvider(value))
        {
        }

        // Enum -> "D" / "B" / "T"
        private static string ToProvider(ConnectionType value) => value.ToCode().ToString();

        // "D" / "B" / "T" -> enum (throws on anything else, so corrupt data is never silently accepted)
        private static ConnectionType FromProvider(string value) => ConnectionTypeExtensions.FromCode(value[0]);
    }
}
