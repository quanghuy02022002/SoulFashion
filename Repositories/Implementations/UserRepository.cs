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

        public Task<List<User>> GetAllAsync()
            => _context.Users.ToListAsync();

        public Task<User?> GetByIdAsync(int id)
            => _context.Users.FindAsync(id).AsTask();

        public Task<User?> GetByEmailAsync(string email)
            => _context.Users.FirstOrDefaultAsync(u => u.Email == email);

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
