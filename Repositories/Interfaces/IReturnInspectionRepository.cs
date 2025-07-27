// Repositories/Interfaces/IReturnInspectionRepository.cs
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IReturnInspectionRepository
    {
        Task<IEnumerable<ReturnInspection>> GetAllAsync();
        Task<ReturnInspection?> GetByIdAsync(int id);
        Task<ReturnInspection?> GetByOrderIdAsync(int orderId);
        Task<ReturnInspection> CreateAsync(ReturnInspection inspection);
        Task UpdateAsync(ReturnInspection inspection);
        Task DeleteAsync(int id);
    }
}
