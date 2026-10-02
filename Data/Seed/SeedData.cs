using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Seed
{
    /// <summary>
    /// STATIC REFERENCE DATA taken from the project specification. It is loaded through
    /// EF Core <c>HasData</c>, so it is created by the migration itself and every database
    /// starts with the same plan catalogue and discount bands.
    ///
    /// Rules for anything added here: fixed primary keys, fixed dates (never DateTime.UtcNow),
    /// and no secrets. Data that needs a password hash or machine-specific values (the first
    /// Admin employee/login, sample cities, retail shops) is NOT seeded here; it belongs in a
    /// runtime seeder added with authentication in Phase 2.
    /// </summary>
    public static class SeedData
    {
        /// <summary>Fixed timestamp stamped on every seeded row (HasData must be deterministic).</summary>
        public static readonly DateTime SeedTimestampUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Security deposits from the specification (per connection type).
        private const decimal DialUpDeposit = 325m;
        private const decimal BroadbandDeposit = 500m;
        private const decimal LandlineDeposit = 250m;

        /// <summary>
        /// The plan catalogue. Ids 1-7 Dial-Up, 8-13 Broadband, 14-18 Landline.
        /// </summary>
        public static Plan[] GetPlans()
        {
            return new[]
            {
                // ---------------- Dial-Up: hourly ----------------
                Internet(1, "Dial-Up Hourly 10 Hrs",  ConnectionType.DialUp, PlanKind.Hourly, validityMonths: 1, price: 50m,  DialUpDeposit, hours: 10),
                Internet(2, "Dial-Up Hourly 30 Hrs",  ConnectionType.DialUp, PlanKind.Hourly, validityMonths: 3, price: 130m, DialUpDeposit, hours: 30),
                Internet(3, "Dial-Up Hourly 60 Hrs",  ConnectionType.DialUp, PlanKind.Hourly, validityMonths: 6, price: 260m, DialUpDeposit, hours: 60),

                // ---------------- Dial-Up: unlimited ----------------
                Internet(4, "Dial-Up Unlimited 28 Kbps Monthly",   ConnectionType.DialUp, PlanKind.Unlimited, validityMonths: 1, price: 75m,  DialUpDeposit, speed: 28),
                Internet(5, "Dial-Up Unlimited 28 Kbps Quarterly", ConnectionType.DialUp, PlanKind.Unlimited, validityMonths: 3, price: 150m, DialUpDeposit, speed: 28),
                Internet(6, "Dial-Up Unlimited 56 Kbps Monthly",   ConnectionType.DialUp, PlanKind.Unlimited, validityMonths: 1, price: 100m, DialUpDeposit, speed: 56),
                Internet(7, "Dial-Up Unlimited 56 Kbps Quarterly", ConnectionType.DialUp, PlanKind.Unlimited, validityMonths: 3, price: 180m, DialUpDeposit, speed: 56),

                // ---------------- Broadband: hourly ----------------
                Internet(8, "Broadband Hourly 30 Hrs", ConnectionType.Broadband, PlanKind.Hourly, validityMonths: 1, price: 175m, BroadbandDeposit, hours: 30),
                Internet(9, "Broadband Hourly 60 Hrs", ConnectionType.Broadband, PlanKind.Hourly, validityMonths: 6, price: 315m, BroadbandDeposit, hours: 60),

                // ---------------- Broadband: unlimited ----------------
                Internet(10, "Broadband Unlimited 64 Kbps Monthly",    ConnectionType.Broadband, PlanKind.Unlimited, validityMonths: 1, price: 225m, BroadbandDeposit, speed: 64),
                Internet(11, "Broadband Unlimited 64 Kbps Quarterly",  ConnectionType.Broadband, PlanKind.Unlimited, validityMonths: 3, price: 400m, BroadbandDeposit, speed: 64),
                Internet(12, "Broadband Unlimited 128 Kbps Monthly",   ConnectionType.Broadband, PlanKind.Unlimited, validityMonths: 1, price: 350m, BroadbandDeposit, speed: 128),
                Internet(13, "Broadband Unlimited 128 Kbps Quarterly", ConnectionType.Broadband, PlanKind.Unlimited, validityMonths: 3, price: 445m, BroadbandDeposit, speed: 128),

                // ---------------- Landline: Local plans (rental + call charges) ----------------
                Landline(14, "Landline Local Unlimited (Yearly rental)", PlanKind.LocalRental, validityMonths: 12, price: 75m,
                         local: 0.55m),
                Landline(15, "Landline Local Monthly", PlanKind.LocalRental, validityMonths: 1, price: 35m,
                         local: 0.75m),

                // ---------------- Landline: STD plans ----------------
                Landline(16, "Landline STD Monthly", PlanKind.StdRental, validityMonths: 1, price: 125m,
                         local: 0.70m, std: 2.25m, mobile: 1.00m),

                // The specification says "valid for a month" for the half-yearly plan; that is a
                // typo in the source document, so six months is used.
                Landline(17, "Landline STD Half-Yearly", PlanKind.StdRental, validityMonths: 6, price: 420m,
                         local: 0.60m, std: 2.00m, mobile: 1.15m),

                // The specification leaves the yearly STD rental price blank. It is seeded INACTIVE at 0
                // so the Admin must enter the real price and activate it.
                Landline(18, "Landline STD Yearly", PlanKind.StdRental, validityMonths: 12, price: 0m,
                         local: 0.60m, std: 1.75m, mobile: 1.25m, isActive: false,
                         description: "PRICE NOT GIVEN IN THE PROJECT SPECIFICATION. Admin must set the yearly rental before activating this plan.")
            };
        }

        /// <summary>
        /// Bulk / corporate discount bands. Both limits are inclusive, so a customer taking exactly
        /// 15 connections falls in the 50% band; the specification's ranges (10-15, 15-25, 25-50,
        /// "above 50") overlap at their ends, and this is the interpretation used.
        /// Fewer than 10 connections earns no discount, so no band exists for that.
        /// </summary>
        public static DiscountScheme[] GetDiscountSchemes()
        {
            return new[]
            {
                Scheme(1, "Bulk 10 to 14 connections", min: 10, max: 14,   percent: 25m),
                Scheme(2, "Bulk 15 to 24 connections", min: 15, max: 24,   percent: 50m),
                Scheme(3, "Bulk 25 to 49 connections", min: 25, max: 49,   percent: 75m),
                Scheme(4, "Bulk 50 or more connections", min: 50, max: null, percent: 100m)
            };
        }

        // ---------------------------------------------------------------- builders
        private static Plan Internet(int id, string name, ConnectionType type, PlanKind kind, int validityMonths,
                                     decimal price, decimal deposit, int? hours = null, int? speed = null)
        {
            return new Plan
            {
                Id = id,
                Name = name,
                ConnectionType = type,
                Kind = kind,
                IncludedHours = hours,
                SpeedKbps = speed,
                ValidityMonths = validityMonths,
                Price = price,
                SecurityDeposit = deposit,
                IsActive = true,
                CreatedAtUtc = SeedTimestampUtc
            };
        }

        private static Plan Landline(int id, string name, PlanKind kind, int validityMonths, decimal price,
                                     decimal local, decimal? std = null, decimal? mobile = null,
                                     bool isActive = true, string? description = null)
        {
            return new Plan
            {
                Id = id,
                Name = name,
                Description = description,
                ConnectionType = ConnectionType.Telephone,
                Kind = kind,
                ValidityMonths = validityMonths,
                Price = price,
                SecurityDeposit = LandlineDeposit,
                LocalCallRatePerMinute = local,
                StdCallRatePerMinute = std,
                MobileMessagingRatePerMinute = mobile,
                IsActive = isActive,
                CreatedAtUtc = SeedTimestampUtc
            };
        }

        private static DiscountScheme Scheme(int id, string name, int min, int? max, decimal percent)
        {
            return new DiscountScheme
            {
                Id = id,
                Name = name,
                MinConnections = min,
                MaxConnections = max,
                DiscountPercent = percent,
                AppliesToAdvance = true,
                AppliesToSecurityDeposit = true,
                IsActive = true,
                CreatedAtUtc = SeedTimestampUtc
            };
        }
    }
}
