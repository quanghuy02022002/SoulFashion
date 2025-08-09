// Repositories/Implementations/ReviewRepository.cs
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDBContext _context;
    public ReviewRepository(AppDBContext context) => _context = context;

    public Task<Review> GetByIdAsync(int id) =>
        _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);

    public Task<List<Review>> GetByCostumeIdAsync(int costumeId, int page, int pageSize) =>
        _context.Reviews
            .Where(r => r.CostumeId == costumeId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

    public Task<int> CountByCostumeIdAsync(int costumeId) =>
        _context.Reviews.CountAsync(r => r.CostumeId == costumeId);

    public async Task<Review> AddAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task UpdateAsync(Review review)
    {
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var r = await _context.Reviews.FindAsync(id);
        if (r == null) return false;
        _context.Reviews.Remove(r);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<Review> GetByUserAndCostumeAsync(int userId, int costumeId) =>
        _context.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.CostumeId == costumeId);
}
