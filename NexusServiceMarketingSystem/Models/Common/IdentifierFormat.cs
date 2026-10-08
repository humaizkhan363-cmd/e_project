using System.Globalization;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Common
{
    /// <summary>
    /// The single definition of the two system-generated identifiers.
    ///
    /// ORDER NUMBER (exactly 11 characters): 1 letter (D / B / T) + 10-digit serial.
    ///     Example: D0000000001
    ///
    /// ACCOUNT ID (exactly 16 characters): 1 letter (D / B / T) + 3-digit city code + 12-digit serial.
    ///     Example: B001000000000001
    ///
    /// The database CHECK constraints, the entity guards and the generator service all read their
    /// rules from here, so the format can never drift between layers.
    /// </summary>
    public static class IdentifierFormat
    {
        // ---- Order number ------------------------------------------------------------
        /// <summary>Total length of an order number.</summary>
        public const int OrderNumberLength = 11;

        /// <summary>Digits in the serial part of an order number (total length minus the type letter).</summary>
        public const int OrderSerialDigits = OrderNumberLength - 1;

        /// <summary>Largest serial an order number can hold (ten nines).</summary>
        public const long MaxOrderSerial = 9_999_999_999L;

        // ---- Account ID --------------------------------------------------------------
        /// <summary>Digits in the city code.</summary>
        public const int CityCodeLength = 3;

        /// <summary>Digits in the serial part of an account ID.</summary>
        public const int AccountSerialDigits = 12;

        /// <summary>Total length of an account ID: type letter + city code + serial.</summary>
        public const int AccountIdLength = 1 + CityCodeLength + AccountSerialDigits;

        /// <summary>Largest serial an account ID can hold (twelve nines).</summary>
        public const long MaxAccountSerial = 999_999_999_999L;

        // ---- SQL LIKE patterns (used by the database CHECK constraints) -------------
        /// <summary>SQL LIKE character class matching exactly one connection-type letter.</summary>
        public const string TypeLetterLikePattern = "[DBT]";

        /// <summary>SQL LIKE pattern accepted for an order number.</summary>
        public static readonly string OrderNumberLikePattern =
            TypeLetterLikePattern + DigitsLikePattern(OrderSerialDigits);

        /// <summary>SQL LIKE pattern accepted for an account ID.</summary>
        public static readonly string AccountIdLikePattern =
            TypeLetterLikePattern + DigitsLikePattern(CityCodeLength + AccountSerialDigits);

        /// <summary>Builds a SQL LIKE pattern that matches exactly <paramref name="count"/> digits.</summary>
        public static string DigitsLikePattern(int count) => string.Concat(Enumerable.Repeat("[0-9]", count));

        // ---- Formatting --------------------------------------------------------------
        /// <summary>Builds an order number from a connection type and a serial (1 to <see cref="MaxOrderSerial"/>).</summary>
        public static string FormatOrderNumber(ConnectionType type, long serial)
        {
            if (serial < 1 || serial > MaxOrderSerial)
            {
                throw new ArgumentOutOfRangeException(nameof(serial), serial,
                    $"Order serial must be between 1 and {MaxOrderSerial}.");
            }

            return type.ToCode() + serial.ToString("D" + OrderSerialDigits, CultureInfo.InvariantCulture);
        }

        /// <summary>Builds an account ID from a connection type, a 3-digit city code and a serial.</summary>
        public static string FormatAccountId(ConnectionType type, string cityCode, long serial)
        {
            if (!IsValidCityCode(cityCode))
            {
                throw new ArgumentException($"City code must be exactly {CityCodeLength} digits.", nameof(cityCode));
            }

            if (serial < 1 || serial > MaxAccountSerial)
            {
                throw new ArgumentOutOfRangeException(nameof(serial), serial,
                    $"Account serial must be between 1 and {MaxAccountSerial}.");
            }

            return type.ToCode() + cityCode + serial.ToString("D" + AccountSerialDigits, CultureInfo.InvariantCulture);
        }

        // ---- Validation --------------------------------------------------------------
        /// <summary>True when the value is exactly three ASCII digits.</summary>
        public static bool IsValidCityCode(string? value) =>
            value is { Length: CityCodeLength } && AllAsciiDigits(value, 0, CityCodeLength);

        /// <summary>True when the value is exactly 11 characters: D/B/T followed by ten digits.</summary>
        public static bool IsValidOrderNumber(string? value) =>
            value is { Length: OrderNumberLength }
            && IsTypeLetter(value[0])
            && AllAsciiDigits(value, 1, OrderSerialDigits);

        /// <summary>True when the value is exactly 16 characters: D/B/T, three city digits, twelve serial digits.</summary>
        public static bool IsValidAccountId(string? value) =>
            value is { Length: AccountIdLength }
            && IsTypeLetter(value[0])
            && AllAsciiDigits(value, 1, CityCodeLength + AccountSerialDigits);

        /// <summary>Extracts the 3-digit city code from a valid account ID.</summary>
        public static string GetCityCode(string accountId)
        {
            if (!IsValidAccountId(accountId))
            {
                throw new ArgumentException("Not a valid account ID.", nameof(accountId));
            }

            return accountId.Substring(1, CityCodeLength);
        }

        private static bool IsTypeLetter(char c) => c is 'D' or 'B' or 'T';

        // Only ASCII 0-9 count as digits (char.IsDigit would also accept other Unicode digits).
        private static bool AllAsciiDigits(string value, int start, int count)
        {
            for (int i = start; i < start + count; i++)
            {
                if (value[i] < '0' || value[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
