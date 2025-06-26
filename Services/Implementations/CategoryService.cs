using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        public CategoryService(ICategoryRepository repo) => _repo = repo;

        public async Task<List<CategoryDTO>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(c => new CategoryDTO
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ParentId = c.ParentId
            }).ToList();
        }

        public async Task<CategoryDTO?> GetByIdAsync(int id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c == null) return null;

            return new CategoryDTO
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ParentId = c.ParentId
            };
        }

        public async Task<CategoryDTO> AddAsync(CategoryDTO dto)
        {
            var cat = new Category
            {
                CategoryName = dto.CategoryName,
                ParentId = dto.ParentId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var result = await _repo.AddAsync(cat);
            dto.CategoryId = result.CategoryId;
            return dto;
        }

        public async Task<CategoryDTO> UpdateAsync(int id, CategoryDTO dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new Exception("Category not found");

            entity.CategoryName = dto.CategoryName;
            entity.ParentId = dto.ParentId;
            entity.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(entity);
            return dto;
        }

        public async Task<bool> DeleteAsync(int id)
            => await _repo.DeleteAsync(id);
    }
}
