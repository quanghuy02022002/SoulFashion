using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface ICostumeRepository
    {
        Task<List<Costume>> GetAllAsync(string? search, int page, int pageSize);
        Task<List<Costume>> GetByUserIdAsync(int userId);
        Task<int> CountAsync(string? search);
        Task<Costume?> GetByIdAsync(int id);
        Task<Costume> AddAsync(Costume costume);
        Task<Costume> UpdateAsync(Costume costume);
        Task<bool> DeleteAsync(int id);

    }
}
