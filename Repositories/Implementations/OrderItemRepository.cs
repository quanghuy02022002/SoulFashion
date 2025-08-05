using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly AppDBContext _context;

        public OrderItemRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OrderItem>> GetAllAsync()
        {
            return await _context.OrderItems
                .Include(i => i.Costume)
                .Include(i => i.Order)
                .ToListAsync();
        }

        public async Task<OrderItem?> GetByIdAsync(int itemId)
        {
            return await _context.OrderItems
                .Include(i => i.Costume)
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.OrderItemId == itemId);
        }

        public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId)
        {
            return await _context.OrderItems
                .Where(x => x.OrderId == orderId)
                .Include(i => i.Costume)
                .ToListAsync();
        }

        public async Task<OrderItem> CreateAsync(OrderItem item)
        {
            _context.OrderItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task UpdateAsync(OrderItem item)
        {
            _context.OrderItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int itemId)
        {
            var item = await _context.OrderItems.FindAsync(itemId);
            if (item != null)
            {
                _context.OrderItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
