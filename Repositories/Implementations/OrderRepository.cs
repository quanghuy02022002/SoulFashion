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
            // Update order chính
            _context.Orders.Update(order);

            // Đảm bảo navigation Deposit được update
            if (order.Deposit != null)
            {
                _context.Entry(order.Deposit).State = EntityState.Modified;
            }

            // Đảm bảo thêm mới status histories nếu có
            if (order.StatusHistories != null)
            {
                foreach (var history in order.StatusHistories)
                {
                    if (history.HistoryId == 0) // mới thêm
                    {
                        _context.OrderStatusHistories.Add(history);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }


        public async Task DeleteAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.CollaboratorEarnings) // Nếu có navigation
                .Include(o => o.Deposit)
                .Include(o => o.StatusHistories)
                .Include(o => o.ReturnInspection)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new Exception("Order not found");

            // Xóa earnings trước (nếu không có navigation thì phải gọi thủ công)
            var earningIds = order.OrderItems.Select(i => i.OrderItemId).ToList();
            var earnings = _context.CollaboratorEarnings
                .Where(e => earningIds.Contains(e.OrderItemId));
            _context.CollaboratorEarnings.RemoveRange(earnings);

            // Xóa các phần liên quan
            if (order.Deposit != null)
                _context.Deposits.Remove(order.Deposit);

            if (order.StatusHistories != null && order.StatusHistories.Any())
                _context.OrderStatusHistories.RemoveRange(order.StatusHistories);

            if (order.ReturnInspection != null)
                _context.ReturnInspections.Remove(order.ReturnInspection);

            if (order.Payments != null && order.Payments.Any())
                _context.Payments.RemoveRange(order.Payments);

            if (order.OrderItems != null && order.OrderItems.Any())
                _context.OrderItems.RemoveRange(order.OrderItems);

            // Xóa Order
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
        }

    }


}
