using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class OrderStatusHistoryDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
