using Repositories.DTOs;
using Repositories.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IOrderItemService
    {
        Task<IEnumerable<OrderItem>> GetAllOrderItemsAsync();

        Task<IEnumerable<OrderItem>> GetItemsByOrderIdAsync(int orderId);
        Task<OrderItem> CreateOrderItemAsync(int orderId, OrderItemDto dto);
        Task DeleteOrderItemAsync(int itemId);
    }
}