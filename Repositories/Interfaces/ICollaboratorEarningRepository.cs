using Repositories.Models;

namespace Repositories.Interfaces;

public interface ICollaboratorEearningRepository // <-- typo? giữ nguyên name đúng: ICollaboratorEarningRepository
{
    Task<(IEnumerable<CollaboratorEarning> Items, long Total)> GetAllAsync(
        int? userId, int? orderId, string? status, int page, int pageSize);

    Task<CollaboratorEarning?> GetByIdAsync(int id);
    Task<CollaboratorEarning> AddAsync(CollaboratorEarning entity);
    Task AddRangeAsync(IEnumerable<CollaboratorEarning> entities);
    Task UpdateAsync(CollaboratorEarning entity);
    Task<bool> DeleteAsync(int id);

    Task<IEnumerable<CollaboratorEarning>> GetByOrderIdAsync(int orderId);
    Task DeleteByOrderIdAsync(int orderId);
}