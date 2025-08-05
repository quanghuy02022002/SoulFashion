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
    public class CostumeRepository : ICostumeRepository
    {
        private readonly AppDBContext _context;

        public CostumeRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<List<Costume>> GetAllAsync(string? search, int page, int pageSize)
        {
            var query = _context.Costumes
                .Include(c => c.CostumeImages)
                .Include(c => c.CreatedBy) // ✅ include người tạo
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(string? search)
        {
            var query = _context.Costumes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
            }

            return await query.CountAsync();
        }

        public async Task<Costume?> GetByIdAsync(int id)
        {
            return await _context.Costumes
                .Include(c => c.CostumeImages)
                .Include(c => c.CreatedBy) // ✅ include người tạo
                .FirstOrDefaultAsync(c => c.CostumeId == id);
        }

        public async Task<Costume> AddAsync(Costume costume)
        {
            _context.Costumes.Add(costume);
            await _context.SaveChangesAsync();
            return costume;
        }

        public async Task<Costume> UpdateAsync(Costume costume)
        {
            _context.Costumes.Update(costume);
            await _context.SaveChangesAsync();
            return costume;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var costume = await _context.Costumes.FindAsync(id);
            if (costume == null) return false;

            _context.Costumes.Remove(costume);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
