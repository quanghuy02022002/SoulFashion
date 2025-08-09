// Repositories/Interfaces/IReviewRepository.cs
using Repositories.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IReviewRepository
    {
        Task<Review> GetByIdAsync(int id);
        Task<List<Review>> GetByCostumeIdAsync(int costumeId, int page, int pageSize);
        Task<int> CountByCostumeIdAsync(int costumeId);
        Task<Review> AddAsync(Review review);
        Task UpdateAsync(Review review);
        Task<bool> DeleteAsync(int id);
        Task<Review> GetByUserAndCostumeAsync(int userId, int costumeId); // để enforce 1 user/1 costume 1 review (nếu cần)
    }
}
