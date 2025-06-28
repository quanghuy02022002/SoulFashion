using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class PaymentDto
    {
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = "vnpay, momo"; // vnpay, momo, zalopay
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = "pending";
        public string? TransactionCode { get; set; }
        public DateTime? PaidAt { get; set; }
    }

}
