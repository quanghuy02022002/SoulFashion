// Services/Interfaces/IReviewService.cs
using Repositories.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewDTO> CreateAsync(ReviewDTO dto);
        Task<ReviewDTO> GetByIdAsync(int id);
        Task<(List<ReviewDTO> Items, int Total)> GetByCostumeAsync(int costumeId, int page, int pageSize);
        Task<bool> UpdateAsync(int id, ReviewDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
