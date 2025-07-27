// Services/Interfaces/IReturnInspectionService.cs
using Repositories.DTOs;
using Repositories.Models;

namespace Services.Interfaces
{
    public interface IReturnInspectionService
    {
        Task<IEnumerable<ReturnInspection>> GetAllAsync();
        Task<ReturnInspection?> GetByIdAsync(int id);
        Task<ReturnInspection?> GetByOrderIdAsync(int orderId);
        Task<ReturnInspection> CreateAsync(ReturnInspectionDto dto);
        Task UpdateAsync(int id, ReturnInspectionDto dto);
        Task DeleteAsync(int id);
    }
}
