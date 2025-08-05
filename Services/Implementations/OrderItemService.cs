using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class OrderItemService : IOrderItemService
    {
        private readonly IOrderItemRepository _repository;
        private readonly ICostumeRepository _costumeRepository;

        public OrderItemService(IOrderItemRepository repository, ICostumeRepository costumeRepository)
        {
            _repository = repository;
            _costumeRepository = costumeRepository;
        }
        public async Task<IEnumerable<OrderItem>> GetAllOrderItemsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<OrderItem>> GetItemsByOrderIdAsync(int orderId) =>
            await _repository.GetByOrderIdAsync(orderId);

        public async Task<OrderItem> CreateOrderItemAsync(int orderId, OrderItemDto dto)
        {
            var costume = await _costumeRepository.GetByIdAsync(dto.CostumeId);
            if (costume == null)
                throw new Exception("Costume không tồn tại");

            var price = dto.IsRental ? costume.PriceRent : costume.PriceSale;
            if (price == null)
                throw new Exception("Costume chưa có giá phù hợp.");

            var item = new OrderItem
            {
                OrderId = orderId,
                CostumeId = dto.CostumeId,
                Quantity = dto.Quantity,
                Price = price.Value,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            return await _repository.CreateAsync(item);
        }

        public async Task DeleteOrderItemAsync(int itemId) =>
            await _repository.DeleteAsync(itemId);
    }
}