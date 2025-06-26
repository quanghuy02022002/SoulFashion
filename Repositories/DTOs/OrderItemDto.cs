using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class OrderItemDto
    {
        public int OrderId { get; set; }
        public int CostumeId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

}
