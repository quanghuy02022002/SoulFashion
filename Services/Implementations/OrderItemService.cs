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
    public class OrderItemService : IOrderItemService
    {
        private readonly IOrderItemRepository _repository;

        public OrderItemService(IOrderItemRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<OrderItem>> GetItemsByOrderIdAsync(int orderId) =>
            await _repository.GetByOrderIdAsync(orderId);

        public async Task<OrderItem> CreateOrderItemAsync(OrderItemDto dto)
        {
            var item = new OrderItem
            {
                OrderId = dto.OrderId,
                CostumeId = dto.CostumeId,
                Quantity = dto.Quantity,
                Price = dto.Price,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            return await _repository.CreateAsync(item);
        }

        public async Task DeleteOrderItemAsync(int itemId) =>
            await _repository.DeleteAsync(itemId);
    }

}
