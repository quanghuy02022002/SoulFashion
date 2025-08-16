using System;

namespace Repositories.DTOs
{
    public class BankTransferVerificationDto
    {
        public int OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; }
        public string? Note { get; set; }
    }
}

