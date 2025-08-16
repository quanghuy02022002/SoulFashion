using System;

namespace Repositories.DTOs
{
    public class BankTransferStatusDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Completed, Failed
        public string? TransactionId { get; set; }
        public DateTime? TransferDate { get; set; }
        public decimal? Amount { get; set; }
        public string? Note { get; set; }
    }
}

