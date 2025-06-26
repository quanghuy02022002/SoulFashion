using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId) =>
            await _context.OrderItems.Where(i => i.OrderId == orderId).ToListAsync();

        public async Task<OrderItem> CreateAsync(OrderItem item)
        {
            _context.OrderItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
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
