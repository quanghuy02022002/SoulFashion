using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;

        public OrderService(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync() =>
            await _repository.GetAllAsync();

        public async Task<Order?> GetOrderByIdAsync(int id) =>
            await _repository.GetByIdAsync(id);

        public async Task<Order> CreateOrderAsync(OrderDto dto)
        {
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                Status = dto.Status,
                TotalPrice = dto.TotalPrice,
                RentStart = dto.RentStart,
                RentEnd = dto.RentEnd,
                IsPaid = dto.IsPaid,
                Note = dto.Note,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            return await _repository.CreateAsync(order);
        }

        public async Task UpdateOrderAsync(int id, OrderDto dto)
        {
            var order = await _repository.GetByIdAsync(id);
            if (order == null) throw new Exception("Order not found");

            order.CustomerId = dto.CustomerId;
            order.Status = dto.Status;
            order.TotalPrice = dto.TotalPrice;
            order.RentStart = dto.RentStart;
            order.RentEnd = dto.RentEnd;
            order.IsPaid = dto.IsPaid;
            order.Note = dto.Note;
            order.UpdatedAt = DateTime.Now;

            await _repository.UpdateAsync(order);
        }

        public async Task DeleteOrderAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _repository.GetByIdAsync(id);
            if (order == null) throw new Exception("Order not found");

            var validStatuses = new[] {
            "pending", "confirmed", "shipped",
            "returned", "completed", "cancelled"
        };

            if (!validStatuses.Contains(status.ToLower()))
                throw new Exception("Invalid status");

            order.Status = status.ToLower();
            order.UpdatedAt = DateTime.Now;

            await _repository.UpdateAsync(order);
        }
    }

}
