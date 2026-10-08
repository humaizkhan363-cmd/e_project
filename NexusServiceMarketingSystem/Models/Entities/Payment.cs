using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A payment against a bill, recorded by a retail employee (at a shop) or by the Accounts
    /// department (at the office). A bill may be paid in several instalments.
    /// </summary>
    public class Payment
    {
        public int Id { get; set; }

        public int BillId { get; set; }
        public Bill Bill { get; set; } = null!;

        /// <summary>Amount received; always greater than zero.</summary>
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }

        /// <summary>Cheque number, card slip or bank reference, if any.</summary>
        public string? Reference { get; set; }

        /// <summary>Set by the database (UTC) when the payment is recorded.</summary>
        public DateTime PaidAtUtc { get; set; }

        public int ReceivedByEmployeeId { get; set; }
        public Employee ReceivedByEmployee { get; set; } = null!;

        /// <summary>The shop where the money was taken; null when paid at the office.</summary>
        public int? RetailShopId { get; set; }
        public RetailShop? RetailShop { get; set; }
    }
}
