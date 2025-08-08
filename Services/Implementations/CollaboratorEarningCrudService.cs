using Repositories.DTOs;
using Repositories.Implementations;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;

public class CollaboratorEarningCrudService : ICollaboratorEarningCrudService
{
    private readonly ICollaboratorEearningRepository _repo;
    private static readonly HashSet<string> _validStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "pending", "paid", "cancelled" };

    public CollaboratorEarningCrudService(ICollaboratorEearningRepository repo) => _repo = repo;

    public async Task<PagedResult<CollaboratorEarningResponseDto>> GetAllAsync(
        int? userId, int? orderId, string? status, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 20;
        if (!string.IsNullOrWhiteSpace(status) && !_validStatuses.Contains(status))
            throw new ArgumentException("Invalid status. Allowed: pending, paid, cancelled");

        var (items, total) = await _repo.GetAllAsync(userId, orderId, status?.ToLower(), page, pageSize);
        return new PagedResult<CollaboratorEarningResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Items = items.Select(Map).ToList()
        };
    }

    public async Task<CollaboratorEarningResponseDto?> GetByIdAsync(int id)
        => (await _repo.GetByIdAsync(id)) is { } e ? Map(e) : null;

    public async Task<CollaboratorEarningResponseDto> CreateAsync(CollaboratorEarningCreateDto dto)
    {
        if (!_validStatuses.Contains(dto.Status))
            throw new ArgumentException("Invalid status. Allowed: pending, paid, cancelled");

        var now = DateTime.Now;
        var entity = new CollaboratorEarning
        {
            UserId = dto.UserId,
            OrderItemId = dto.OrderItemId,
            EarningAmount = dto.EarningAmount,
            Status = dto.Status.ToLower(),
            PaidAt = dto.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };
        var created = await _repo.AddAsync(entity);
        return Map(created);
    }

    public async Task<CollaboratorEarningResponseDto?> UpdateAsync(int id, CollaboratorEarningUpdateDto dto)
    {
        if (!_validStatuses.Contains(dto.Status))
            throw new ArgumentException("Invalid status. Allowed: pending, paid, cancelled");

        var e = await _repo.GetByIdAsync(id);
        if (e == null) return null;

        e.UserId = dto.UserId;
        e.OrderItemId = dto.OrderItemId;
        e.EarningAmount = dto.EarningAmount;
        e.Status = dto.Status.ToLower();
        e.PaidAt = e.Status == "paid" ? (e.PaidAt ?? DateTime.Now) : null;
        e.UpdatedAt = DateTime.Now;

        await _repo.UpdateAsync(e);
        return Map(e);
    }

    public async Task<CollaboratorEarningResponseDto?> PatchStatusAsync(int id, string status)
    {
        if (!_validStatuses.Contains(status))
            throw new ArgumentException("Invalid status. Allowed: pending, paid, cancelled");

        var e = await _repo.GetByIdAsync(id);
        if (e == null) return null;
        e.Status = status.ToLower();
        e.PaidAt = e.Status == "paid" ? (e.PaidAt ?? DateTime.Now) : null;
        e.UpdatedAt = DateTime.Now;

        await _repo.UpdateAsync(e);
        return Map(e);
    }

    public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);

    private static CollaboratorEarningResponseDto Map(CollaboratorEarning e) => new()
    {
        EarningId = e.EarningId,
        UserId = e.UserId,
        OrderItemId = e.OrderItemId,
        EarningAmount = e.EarningAmount,
        Status = e.Status,
        PaidAt = e.PaidAt,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}