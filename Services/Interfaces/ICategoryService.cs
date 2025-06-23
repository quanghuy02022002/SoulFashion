using Repositories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDTO>> GetAllAsync();
        Task<CategoryDTO?> GetByIdAsync(int id);
        Task<CategoryDTO> AddAsync(CategoryDTO dto);
        Task<CategoryDTO> UpdateAsync(int id, CategoryDTO dto);
        Task<bool> DeleteAsync(int id);
    }

}
