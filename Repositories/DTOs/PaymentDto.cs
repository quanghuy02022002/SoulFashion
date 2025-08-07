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
            public string PaymentMethod { get; set; }
            public string PaymentStatus { get; set; }  // e.g. "pending"
            public DateTime? PaidAt { get; set; }
        

    }

}
