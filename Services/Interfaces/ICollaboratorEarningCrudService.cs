using Repositories.DTOs;

namespace Services.Interfaces;

public interface ICollaboratorEarningCrudService
{
    Task<PagedResult<CollaboratorEarningResponseDto>> GetAllAsync(
        int? userId, int? orderId, string? status, int page, int pageSize);

    Task<CollaboratorEarningResponseDto?> GetByIdAsync(int id);
    Task<CollaboratorEarningResponseDto> CreateAsync(CollaboratorEarningCreateDto dto);
    Task<CollaboratorEarningResponseDto?> UpdateAsync(int id, CollaboratorEarningUpdateDto dto);
    Task<CollaboratorEarningResponseDto?> PatchStatusAsync(int id, string status);
    Task<bool> DeleteAsync(int id);
}