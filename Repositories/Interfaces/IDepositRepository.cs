using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IDepositRepository
    {
        Task<IEnumerable<Deposit>> GetAllAsync();
        Task<Deposit?> GetByIdAsync(int id);
        Task<Deposit> CreateAsync(Deposit deposit);
        Task UpdateAsync(Deposit deposit);
        Task DeleteAsync(int id);
    }
}
