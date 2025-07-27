using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IOrderStatusHistoryRepository
    {
        Task<IEnumerable<OrderStatusHistory>> GetAllAsync();
        Task<OrderStatusHistory?> GetByIdAsync(int id);
        Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId);
        Task<OrderStatusHistory> CreateAsync(OrderStatusHistory history);
        Task DeleteAsync(int id);
    }
}
