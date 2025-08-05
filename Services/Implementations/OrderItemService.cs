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

        public async Task<IEnumerable<OrderItemResponseDto>> GetAllOrderItemsAsync()
        {
            var items = await _repository.GetAllAsync();

            return items.Select(item => new OrderItemResponseDto
            {
                OrderItemId = item.OrderItemId,
                OrderId = item.OrderId,
                CostumeId = item.CostumeId,
                CostumeName = item.Costume?.Name,
                CostumeImageUrl = item.Costume?.CostumeImages?.FirstOrDefault()?.ImageUrl ?? "",
                Quantity = item.Quantity,
                Price = item.Price,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            });
        }

        public async Task<OrderItemResponseDto?> GetOrderItemByIdAsync(int itemId)
        {
            var item = await _repository.GetByIdAsync(itemId);
            if (item == null) return null;

            return new OrderItemResponseDto
            {
                OrderItemId = item.OrderItemId,
                OrderId = item.OrderId,
                CostumeId = item.CostumeId,
                CostumeName = item.Costume?.Name,
                CostumeImageUrl = item.Costume?.CostumeImages?.FirstOrDefault()?.ImageUrl ?? "",
                Quantity = item.Quantity,
                Price = item.Price,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }

        public async Task<IEnumerable<OrderItemResponseDto>> GetItemsByOrderIdAsync(int orderId)
        {
            var items = await _repository.GetByOrderIdAsync(orderId);

            return items.Select(item => new OrderItemResponseDto
            {
                OrderItemId = item.OrderItemId,
                OrderId = item.OrderId,
                CostumeId = item.CostumeId,
                CostumeName = item.Costume?.Name,
                CostumeImageUrl = item.Costume?.CostumeImages?.FirstOrDefault()?.ImageUrl ?? "",
                Quantity = item.Quantity,
                Price = item.Price,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            });
        }

        public async Task<OrderItem> CreateOrderItemAsync(int orderId, OrderItemDto dto)
        {
            var costume = await _costumeRepository.GetByIdAsync(dto.CostumeId);
            if (costume == null)
                throw new Exception("❌ Costume không tồn tại");

            var price = dto.IsRental ? costume.PriceRent : costume.PriceSale;
            if (price == null)
                throw new Exception("❌ Costume chưa có giá phù hợp");

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

        public async Task UpdateOrderItemAsync(int itemId, OrderItemDto dto)
        {
            var item = await _repository.GetByIdAsync(itemId);
            if (item == null)
                throw new Exception("❌ OrderItem không tồn tại");

            var costume = await _costumeRepository.GetByIdAsync(dto.CostumeId);
            if (costume == null)
                throw new Exception("❌ Costume không tồn tại");

            var price = dto.IsRental ? costume.PriceRent : costume.PriceSale;
            if (price == null)
                throw new Exception("❌ Costume chưa có giá phù hợp");

            item.CostumeId = dto.CostumeId;
            item.Quantity = dto.Quantity;
            item.Price = price.Value;
            item.UpdatedAt = DateTime.Now;

            await _repository.UpdateAsync(item);
        }

        public async Task DeleteOrderItemAsync(int itemId)
        {
            await _repository.DeleteAsync(itemId);
        }
    }
}
