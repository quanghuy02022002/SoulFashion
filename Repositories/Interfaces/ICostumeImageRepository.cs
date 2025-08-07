using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface ICostumeImageRepository
    {
        Task<List<CostumeImage>> GetByCostumeIdAsync(int costumeId);
        Task<CostumeImage?> GetByIdAsync(int id);
        Task<CostumeImage> AddAsync(CostumeImage image);
        Task UnsetMainImageAsync(int costumeId);
        Task UpdateAsync(CostumeImage image);

        Task<bool> DeleteAsync(int id);
    }
}
