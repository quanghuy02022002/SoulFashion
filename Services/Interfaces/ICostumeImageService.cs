using Repositories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICostumeImageService
    {
        Task<List<CostumeImageDTO>> GetByCostumeIdAsync(int costumeId);
        Task<CostumeImageDTO?> GetByIdAsync(int id);
        Task<CostumeImageDTO> AddAsync(CostumeImageDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
