using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class DepositDto
    {
        public int OrderId { get; set; }
        public decimal DepositAmount { get; set; }
        public string PaymentMethod { get; set; } = "cash";
        public string DepositStatus { get; set; } = "pending";
    }
}
