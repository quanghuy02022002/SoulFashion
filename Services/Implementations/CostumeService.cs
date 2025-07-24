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
    public class CostumeService : ICostumeService
    {
        private readonly ICostumeRepository _repository;

        public CostumeService(ICostumeRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CostumeDTO>> GetAllAsync(string? search, int page, int pageSize)
        {
            var items = await _repository.GetAllAsync(search, page, pageSize);

            return items.Select(c => new CostumeDTO
            {
                CostumeId = c.CostumeId,
                Name = c.Name,
                Description = c.Description,
                CategoryId = c.CategoryId,
                OwnerId = c.OwnerId,
                PriceSale = c.PriceSale,
                PriceRent = c.PriceRent,
                Quantity = (int)c.Quantity,
                Size = c.Size,
                Condition = c.Condition,
                Gender = c.Gender,
                IsActive = (bool)c.IsActive,

                // ✅ Thêm ánh xạ hình ảnh
                Images = c.CostumeImages?.Select(i => new CostumeImageDTO
                {
                    ImageId = i.ImageId,
                    CostumeId = i.CostumeId,
                    ImageUrl = i.ImageUrl,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                }).ToList() ?? new List<CostumeImageDTO>()
            }).ToList();
        }


        public async Task<int> CountAsync(string? search)
        {
            return await _repository.CountAsync(search);
        }

        public async Task<CostumeDTO?> GetByIdAsync(int id)
        {
            var c = await _repository.GetByIdAsync(id);
            if (c == null) return null;

            return new CostumeDTO
            {
                CostumeId = c.CostumeId,
                Name = c.Name,
                Description = c.Description,
                CategoryId = c.CategoryId,
                OwnerId = c.OwnerId,
                PriceSale = c.PriceSale,
                PriceRent = c.PriceRent,
                Quantity = (int)c.Quantity,
                Size = c.Size,
                Condition = c.Condition,
                Gender = c.Gender,
                IsActive = (bool)c.IsActive
            };
        }

        public async Task<CostumeDTO> AddAsync(CostumeDTO dto)
        {
            var c = new Costume
            {
                Name = dto.Name,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                OwnerId = dto.OwnerId,
                PriceSale = dto.PriceSale,
                PriceRent = dto.PriceRent,
                Quantity = dto.Quantity,
                Size = dto.Size,
                Condition = dto.Condition,
                Gender = dto.Gender,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var result = await _repository.AddAsync(c);
            dto.CostumeId = result.CostumeId;
            return dto;
        }

        public async Task<CostumeDTO> UpdateAsync(int id, CostumeDTO dto)
        {
            var costume = await _repository.GetByIdAsync(id);
            if (costume == null) throw new Exception("Not found");

            costume.Name = dto.Name;
            costume.Description = dto.Description;
            costume.CategoryId = dto.CategoryId;
            costume.OwnerId = dto.OwnerId;
            costume.PriceSale = dto.PriceSale;
            costume.PriceRent = dto.PriceRent;
            costume.Quantity = dto.Quantity;
            costume.Size = dto.Size;
            costume.Condition = dto.Condition;
            costume.Gender = dto.Gender;
            costume.IsActive = dto.IsActive;
            costume.UpdatedAt = DateTime.Now;

            await _repository.UpdateAsync(costume);
            return dto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }

}
