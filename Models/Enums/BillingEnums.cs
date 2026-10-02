namespace NexusServiceMarketingSystem.Models.Enums
{
    /// <summary>Payment state of a bill. Amounts paid are always the sum of its Payment rows.</summary>
    public enum BillStatus
    {
        Issued = 1,
        PartiallyPaid = 2,
        Paid = 3,
        Cancelled = 4
    }

    /// <summary>How a payment was made.</summary>
    public enum PaymentMethod
    {
        Cash = 1,
        Card = 2,
        BankTransfer = 3,
        Cheque = 4,
        Online = 5
    }
}
