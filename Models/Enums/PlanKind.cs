namespace NexusServiceMarketingSystem.Models.Enums
{
    /// <summary>How a plan is priced. Drives which optional plan columns must be filled in.</summary>
    public enum PlanKind
    {
        /// <summary>Internet plan with a fixed number of hours (needs IncludedHours).</summary>
        Hourly = 1,

        /// <summary>Internet plan with unlimited use at a given speed (needs SpeedKbps).</summary>
        Unlimited = 2,

        /// <summary>Landline "Local" plan: rental + local call charges.</summary>
        LocalRental = 3,

        /// <summary>Landline "STD" plan: rental + local / STD / mobile-messaging call charges.</summary>
        StdRental = 4
    }
}
