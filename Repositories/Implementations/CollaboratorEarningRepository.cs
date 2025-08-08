using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations;

public class CollaboratorEarningRepository : ICollaboratorEearningRepository
{
    private readonly AppDBContext _context;
    public CollaboratorEarningRepository(AppDBContext context) => _context = context;

    public async Task<(IEnumerable<CollaboratorEarning> Items, long Total)> GetAllAsync(
        int? userId, int? orderId, string? status, int page, int pageSize)
    {
        var q = _context.CollaboratorEarnings.AsNoTracking().AsQueryable();
        if (userId.HasValue) q = q.Where(e => e.UserId == userId.Value);
        if (orderId.HasValue)
        {
            var itemIds = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId.Value)
                .Select(oi => oi.OrderItemId)
                .ToListAsync();
            q = q.Where(e => itemIds.Contains(e.OrderItemId));
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(e => e.Status == status.ToLower());

        var total = await q.LongCountAsync();
        var items = await q.OrderByDescending(e => e.CreatedAt)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync();
        return (items, total);
    }

    public async Task<CollaboratorEarning?> GetByIdAsync(int id)
        => await _context.CollaboratorEarnings.FirstOrDefaultAsync(e => e.EarningId == id);

    public async Task<CollaboratorEarning> AddAsync(CollaboratorEarning entity)
    {
        _context.CollaboratorEarnings.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<CollaboratorEarning> entities)
    {
        _context.CollaboratorEarnings.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CollaboratorEarning entity)
    {
        _context.CollaboratorEarnings.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var e = await _context.CollaboratorEarnings.FindAsync(id);
        if (e == null) return false;
        _context.CollaboratorEarnings.Remove(e);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CollaboratorEarning>> GetByOrderIdAsync(int orderId)
    {
        var ids = await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Select(oi => oi.OrderItemId)
            .ToListAsync();
        return await _context.CollaboratorEarnings.Where(e => ids.Contains(e.OrderItemId)).ToListAsync();
    }

    public async Task DeleteByOrderIdAsync(int orderId)
    {
        var ids = await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Select(oi => oi.OrderItemId)
            .ToListAsync();
        var rows = _context.CollaboratorEarnings.Where(e => ids.Contains(e.OrderItemId));
        _context.CollaboratorEarnings.RemoveRange(rows);
        await _context.SaveChangesAsync();
    }
}