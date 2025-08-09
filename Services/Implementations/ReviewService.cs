// Services/Implementations/ReviewService.cs
using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _repo;

        public ReviewService(IReviewRepository repo)
        {
            _repo = repo;
        }

        public async Task<ReviewDTO> CreateAsync(ReviewDTO dto)
        {
            // (Optional) 1 user chỉ được review 1 lần / costume
            var existed = await _repo.GetByUserAndCostumeAsync(dto.UserId, dto.CostumeId);
            if (existed != null)
                throw new InvalidOperationException("User has already reviewed this costume.");

            var entity = new Review
            {
                UserId = dto.UserId,
                CostumeId = dto.CostumeId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var saved = await _repo.AddAsync(entity);
            return Map(saved);
        }

        public async Task<ReviewDTO> GetByIdAsync(int id)
        {
            var r = await _repo.GetByIdAsync(id);
            return r == null ? null : Map(r);
        }

        public async Task<(List<ReviewDTO> Items, int Total)> GetByCostumeAsync(int costumeId, int page, int pageSize)
        {
            var items = await _repo.GetByCostumeIdAsync(costumeId, page, pageSize);
            var total = await _repo.CountByCostumeIdAsync(costumeId);
            return (items.Select(Map).ToList(), total);
        }

        public async Task<bool> UpdateAsync(int id, ReviewDTO dto)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return false;

            r.Rating = dto.Rating;
            r.Comment = dto.Comment;
            r.UpdatedAt = DateTime.Now;
            await _repo.UpdateAsync(r);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repo.DeleteAsync(id);
        }

        private static ReviewDTO Map(Review r) => new ReviewDTO
        {
            ReviewId = r.ReviewId,
            UserId = r.UserId,
            CostumeId = r.CostumeId,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
