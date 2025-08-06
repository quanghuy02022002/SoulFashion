using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class CartItemRepository : ICartItemRepository
    {
        private readonly AppDBContext _context;

        public CartItemRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CartItem>> GetAllAsync()
        {
            return await _context.CartItems
                .Include(x => x.Costume)
                .Include(x => x.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<CartItem>> GetByUserIdAsync(int userId)
        {
            return await _context.CartItems
                .Where(x => x.UserId == userId)
                .Include(x => x.Costume)
                .ToListAsync();
        }
        

        public async Task<CartItem?> GetByUserAndCostumeAsync(int userId, int costumeId)
        {
            return await _context.CartItems
                .FirstOrDefaultAsync(x => x.UserId == userId && x.CostumeId == costumeId);
        }

        public async Task<CartItem> AddAsync(CartItem item)
        {
            _context.CartItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task UpdateAsync(CartItem item)
        {
            _context.CartItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public Task<CartItem?> GetByIdAsync(int cartItemId)
        {
            return _context.CartItems
                .Include(x => x.Costume)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId);
        }
    }
}