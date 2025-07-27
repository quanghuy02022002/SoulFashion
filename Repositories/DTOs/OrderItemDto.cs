using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    
        public class OrderItemDto
        {
            public int CostumeId { get; set; }
            public int Quantity { get; set; }
            public bool IsRental { get; set; } // ✔ Phân biệt môn thuê hay mua
        }
    

}
