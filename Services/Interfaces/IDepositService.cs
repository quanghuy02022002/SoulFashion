using Repositories.DTOs;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IDepositService
    {
        Task<IEnumerable<Deposit>> GetAllAsync();
        Task<Deposit?> GetByIdAsync(int id);
        Task<Deposit> CreateAsync(DepositDto dto);
        Task UpdateAsync(int id, DepositDto dto);
        Task DeleteAsync(int id);
    }
}
