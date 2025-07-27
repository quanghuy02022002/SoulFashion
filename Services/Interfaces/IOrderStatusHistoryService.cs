// Services/Interfaces/IOrderStatusHistoryService.cs
using Repositories.DTOs;
using Repositories.Models;

namespace Services.Interfaces
{
    public interface IOrderStatusHistoryService
    {
        Task<IEnumerable<OrderStatusHistory>> GetAllAsync();
        Task<OrderStatusHistory?> GetByIdAsync(int id);
        Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId);
        Task<OrderStatusHistory> CreateAsync(OrderStatusHistoryDto dto);
        Task DeleteAsync(int id);
    }
}
