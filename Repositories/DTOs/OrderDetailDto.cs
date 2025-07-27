// Repositories/DTOs/OrderDetailDto.cs
using System;
using System.Collections.Generic;

namespace Repositories.DTOs
{
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? RentStart { get; set; }
        public DateTime? RentEnd { get; set; }
        public decimal? TotalPrice { get; set; }
        public bool IsPaid { get; set; }
        public string? Note { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
        public DepositDto? Deposit { get; set; }
        public ReturnInspectionDto? ReturnInfo { get; set; }
        public List<OrderStatusHistoryDto> StatusHistories { get; set; } = new();
    }
}
