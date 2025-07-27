using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class OrderStatusHistory
    {
        [Key]
        public int HistoryId { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.Now;
        public string? Note { get; set; }

        public Order? Order { get; set; }
    }

}
