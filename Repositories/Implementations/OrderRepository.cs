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
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDBContext _context;

        public OrderRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders
                .AsNoTracking()
                .ToListAsync();
        }



        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Include(o => o.Deposit)
                .Include(o => o.StatusHistories)
                .Include(o => o.ReturnInspection)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int orderId)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            // Lấy danh sách OrderItemId của order
            var itemIds = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => oi.OrderItemId)
                .ToListAsync();

            // 1) Xóa earnings theo itemIds
            await _context.CollaboratorEarnings
                .Where(e => itemIds.Contains(e.OrderItemId))
                .ExecuteDeleteAsync();

            // 2) Xóa các bảng liên quan tới order
            await _context.Deposits.Where(d => d.OrderId == orderId).ExecuteDeleteAsync();
            await _context.OrderStatusHistories.Where(s => s.OrderId == orderId).ExecuteDeleteAsync();
            await _context.ReturnInspections.Where(r => r.OrderId == orderId).ExecuteDeleteAsync();
            await _context.Payments.Where(p => p.OrderId == orderId).ExecuteDeleteAsync();
            await _context.OrderItems.Where(oi => oi.OrderId == orderId).ExecuteDeleteAsync();

            // 3) Xóa chính Order
            await _context.Orders.Where(o => o.OrderId == orderId).ExecuteDeleteAsync();

            await tx.CommitAsync();
        }
        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == userId)
                .Include(o => o.OrderItems)
                .Include(o => o.Deposit)
                .Include(o => o.StatusHistories)
                .AsNoTracking()
                .ToListAsync();
        }

    }


}
