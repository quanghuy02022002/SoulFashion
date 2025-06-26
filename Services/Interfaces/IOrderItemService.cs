using Repositories.DTOs;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IOrderItemService
    {
        Task<IEnumerable<OrderItem>> GetItemsByOrderIdAsync(int orderId);
        Task<OrderItem> CreateOrderItemAsync(OrderItemDto dto);
        Task DeleteOrderItemAsync(int itemId);
    }

}
