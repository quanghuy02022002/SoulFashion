using Repositories.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IOrderItemRepository
    {
        Task<IEnumerable<OrderItem>> GetAllAsync();
        Task<OrderItem?> GetByIdAsync(int itemId);
        Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId);
        Task<OrderItem> CreateAsync(OrderItem item);
        Task UpdateAsync(OrderItem item); // 👈 thêm Update
        Task DeleteAsync(int itemId);
    }
}
