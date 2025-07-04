﻿using System;
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
        public decimal TotalPrice { get; set; }
        public DateTime? RentStart { get; set; }
        public DateTime? RentEnd { get; set; }
        public bool IsPaid { get; set; } = false;
        public string? Note { get; set; }
    }

}
