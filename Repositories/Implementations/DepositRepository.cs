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
    public class DepositRepository : IDepositRepository
    {
        private readonly AppDBContext _context;
        public DepositRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Deposit>> GetAllAsync()
            => await _context.Deposits.ToListAsync();

        public async Task<Deposit?> GetByIdAsync(int id)
            => await _context.Deposits.FindAsync(id);

        public async Task<Deposit> CreateAsync(Deposit deposit)
        {
            _context.Deposits.Add(deposit);
            await _context.SaveChangesAsync();
            return deposit;
        }

        public async Task UpdateAsync(Deposit deposit)
        {
            _context.Deposits.Update(deposit);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var deposit = await _context.Deposits.FindAsync(id);
            if (deposit != null)
            {
                _context.Deposits.Remove(deposit);
                await _context.SaveChangesAsync();
            }
        }
    }
}
