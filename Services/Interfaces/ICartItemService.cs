using Repositories.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICartItemService
    {
        Task<IEnumerable<CartItem>> GetAllCartItemsAsync();
        Task<IEnumerable<CartItem>> GetCartItemsByUserAsync(int userId);
        Task<CartItem> AddOrUpdateCartItemAsync(int userId, int costumeId, int quantity);
        Task<CartItem> UpdateCartItemQuantityAsync(int cartItemId, int newQuantity);
        Task DeleteCartItemAsync(int cartItemId);
        Task DeleteAllCartItemsByUserAsync(int userId);

    }
}