// Repositories/Implementations/OrderStatusHistoryRepository.cs
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class OrderStatusHistoryRepository : IOrderStatusHistoryRepository
    {
        private readonly AppDBContext _context;
        public OrderStatusHistoryRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OrderStatusHistory>> GetAllAsync()
            => await _context.OrderStatusHistories.ToListAsync();

        public async Task<OrderStatusHistory?> GetByIdAsync(int id)
            => await _context.OrderStatusHistories.FindAsync(id);

        public async Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId)
            => await _context.OrderStatusHistories
                            .Where(h => h.OrderId == orderId)
                            .OrderByDescending(h => h.ChangedAt)
                            .ToListAsync();

        public async Task<OrderStatusHistory> CreateAsync(OrderStatusHistory history)
        {
            _context.OrderStatusHistories.Add(history);
            await _context.SaveChangesAsync();
            return history;
        }

        public async Task DeleteAsync(int id)
        {
            var history = await _context.OrderStatusHistories.FindAsync(id);
            if (history != null)
            {
                _context.OrderStatusHistories.Remove(history);
                await _context.SaveChangesAsync();
            }
        }
    }
}
