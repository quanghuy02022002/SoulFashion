using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class OrderDto
    {
        public int CustomerId { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime? RentStart { get; set; }
        public DateTime? RentEnd { get; set; }
        public bool IsPaid { get; set; } = false;
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "cash";
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string ShippingAddress { get; set; }
        public bool? HasIdentityCard { get; set; }

        public List<OrderItemDto> Items { get; set; } = new(); // ✅ Bắt buộc
    }



}
