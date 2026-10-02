using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Data.Converters;
using NexusServiceMarketingSystem.Models.Common;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>
    /// Small helpers shared by the entity configurations, so column types and CHECK-constraint
    /// SQL are written once.
    /// </summary>
    internal static class ConfigurationHelpers
    {
        /// <summary>
        /// Binary collation used for identifier CHECK constraints. The database default collation
        /// is case-insensitive, which would wrongly accept 'd0000000001'; binary makes it exact.
        /// </summary>
        public const string BinaryCollation = "Latin1_General_100_BIN2";

        /// <summary>Stores an enum as readable text (varchar) instead of a number.</summary>
        public static PropertyBuilder<TEnum> HasStringConversion<TEnum>(this PropertyBuilder<TEnum> property, int maxLength = 30)
            where TEnum : struct, Enum
        {
            return property.HasConversion<string>().HasMaxLength(maxLength).IsUnicode(false);
        }

        /// <summary>Stores a <see cref="ConnectionType"/> as a one-letter char(1): 'D', 'B' or 'T'.</summary>
        public static PropertyBuilder<ConnectionType> HasConnectionTypeCode(this PropertyBuilder<ConnectionType> property)
        {
            return property.HasConversion(new ConnectionTypeConverter()).HasColumnType("char(1)");
        }

        /// <summary>CHECK text: the column must be one of the enum's names, e.g. [Status] IN ('Placed', 'Feasible').</summary>
        public static string EnumInList<TEnum>(string column) where TEnum : struct, Enum
        {
            string names = string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"));
            return $"[{column}] IN ({names})";
        }

        /// <summary>CHECK text: the column must match a SQL LIKE pattern, compared case-sensitively.</summary>
        public static string ExactLike(string column, string likePattern)
        {
            return $"[{column}] COLLATE {BinaryCollation} LIKE '{likePattern}'";
        }

        /// <summary>CHECK text: the column holds one of the connection-type letters D, B or T.</summary>
        public static string ConnectionTypeLetter(string column)
        {
            return ExactLike(column, IdentifierFormat.TypeLetterLikePattern);
        }
    }
}
