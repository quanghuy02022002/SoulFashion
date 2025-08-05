using Repositories.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface ICartItemRepository
    {
        Task<IEnumerable<CartItem>> GetAllAsync();
        Task<IEnumerable<CartItem>> GetByUserIdAsync(int userId);
        Task<CartItem?> GetByUserAndCostumeAsync(int userId, int costumeId);
        Task<CartItem> AddAsync(CartItem item);
        Task UpdateAsync(CartItem item);
        Task DeleteAsync(int cartItemId);
    }
}
