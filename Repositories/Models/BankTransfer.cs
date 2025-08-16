using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class BankTransfer
    {
        [Key]
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;
        
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}

