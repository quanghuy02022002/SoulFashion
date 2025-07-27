using Repositories.DTOs;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderSummaryDto>> GetAllOrdersAsync();
        Task<Order?> GetOrderByIdAsync(int id);
        Task<OrderDetailDto> GetOrderDetailAsync(int id);
        Task<Order> CreateOrderAsync(OrderDto dto);
        Task UpdateOrderAsync(int id, OrderDto dto);
        Task DeleteOrderAsync(int id);
        Task UpdateOrderStatusAsync(int id, string status);
    }

}
