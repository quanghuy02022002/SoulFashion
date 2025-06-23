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
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDBContext _context;
        public CategoryRepository(AppDBContext context) => _context = context;

        public async Task<List<Category>> GetAllAsync()
            => await _context.Categories.Include(c => c.InverseParent).ToListAsync();

        public async Task<Category?> GetByIdAsync(int id)
            => await _context.Categories.FindAsync(id);

        public async Task<Category> AddAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return false;

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
