using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(int id);
        Task<Order> CreateAsync(Order order);
        Task<IEnumerable<Order>> GetByUserIdAsync(int userId); // 🆕
        Task UpdateAsync(Order order);
        Task DeleteAsync(int id);
    }

}
