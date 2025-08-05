using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class CartItemService : ICartItemService
    {
        private readonly ICartItemRepository _repository;

        public CartItemService(ICartItemRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CartItem>> GetAllCartItemsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<CartItem>> GetCartItemsByUserAsync(int userId)
        {
            return await _repository.GetByUserIdAsync(userId);
        }

        public async Task<CartItem> AddOrUpdateCartItemAsync(int userId, int costumeId, int quantity)
        {
            var existing = await _repository.GetByUserAndCostumeAsync(userId, costumeId);

            if (existing != null)
            {
                existing.Quantity += quantity;
                await _repository.UpdateAsync(existing);
                return existing;
            }

            var newItem = new CartItem
            {
                UserId = userId,
                CostumeId = costumeId,
                Quantity = quantity,
                CreatedAt = DateTime.Now
            };

            return await _repository.AddAsync(newItem);
        }

        public async Task DeleteCartItemAsync(int cartItemId)
        {
            await _repository.DeleteAsync(cartItemId);
        }
    }
}
