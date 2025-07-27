// Repositories/Implementations/ReturnInspectionRepository.cs
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class ReturnInspectionRepository : IReturnInspectionRepository
    {
        private readonly AppDBContext _context;
        public ReturnInspectionRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReturnInspection>> GetAllAsync()
            => await _context.ReturnInspections.ToListAsync();

        public async Task<ReturnInspection?> GetByIdAsync(int id)
            => await _context.ReturnInspections.FindAsync(id);

        public async Task<ReturnInspection?> GetByOrderIdAsync(int orderId)
            => await _context.ReturnInspections.FirstOrDefaultAsync(r => r.OrderId == orderId);

        public async Task<ReturnInspection> CreateAsync(ReturnInspection inspection)
        {
            _context.ReturnInspections.Add(inspection);
            await _context.SaveChangesAsync();
            return inspection;
        }

        public async Task UpdateAsync(ReturnInspection inspection)
        {
            _context.ReturnInspections.Update(inspection);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var inspection = await _context.ReturnInspections.FindAsync(id);
            if (inspection != null)
            {
                _context.ReturnInspections.Remove(inspection);
                await _context.SaveChangesAsync();
            }
        }
    }
}
