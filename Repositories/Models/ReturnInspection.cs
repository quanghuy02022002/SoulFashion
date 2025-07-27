using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ReturnInspection
    {
        [Key]
        public int InspectionId { get; set; }
        public int OrderId { get; set; }
        public string Condition { get; set; } = string.Empty;
        public decimal PenaltyAmount { get; set; }
        public string? Note { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.Now;

        public Order? Order { get; set; }
    }
}
