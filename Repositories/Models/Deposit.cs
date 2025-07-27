using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
namespace Repositories.Models
{
    public class Deposit
    {
        [Key]
        public int DepositId { get; set; }
        public int OrderId { get; set; }
        public decimal DepositAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string DepositStatus { get; set; } = "pending"; // paid, refunded, held
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public Order? Order { get; set; }
    }
}
