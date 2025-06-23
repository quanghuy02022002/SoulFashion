using Repositories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICostumeService
    {
        Task<List<CostumeDTO>> GetAllAsync(string? search, int page, int pageSize);
        Task<int> CountAsync(string? search);
        Task<CostumeDTO?> GetByIdAsync(int id);
        Task<CostumeDTO> AddAsync(CostumeDTO dto);
        Task<CostumeDTO> UpdateAsync(int id, CostumeDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
