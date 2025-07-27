using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
        public decimal? TotalPrice { get; set; }
        public DateTime? RentStart { get; set; }
        public DateTime? RentEnd { get; set; }
        public bool IsPaid { get; set; }
        public int CustomerId { get; set; }
    }

}
