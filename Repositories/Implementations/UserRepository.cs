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
    public class UserRepository : IUserRepository
    {
        private readonly AppDBContext _context;

        public UserRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.UserVerification)
                .ToListAsync();
        }


        public Task<User?> GetByIdAsync(int id)
        {
            return _context.Users
                .Include(u => u.UserVerification)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }


        public Task<User?> GetByEmailAsync(string email)
        {
            return _context.Users
                .Include(u => u.UserVerification)
                .FirstOrDefaultAsync(u => u.Email == email);
        }


        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public Task SaveAsync()
            => _context.SaveChangesAsync();
    }

}
