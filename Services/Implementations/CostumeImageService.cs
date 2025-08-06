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
    public class CostumeImageService : ICostumeImageService
    {
        private readonly ICostumeImageRepository _repository;

        public CostumeImageService(ICostumeImageRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CostumeImageDTO>> GetByCostumeIdAsync(int costumeId)
        {
            var images = await _repository.GetByCostumeIdAsync(costumeId);
            return images.Select(i => new CostumeImageDTO
            {
                ImageId = i.ImageId,
                CostumeId = i.CostumeId,
                ImageUrl = i.ImageUrl
            }).ToList();
        }

        public async Task<CostumeImageDTO?> GetByIdAsync(int id)
        {
            var img = await _repository.GetByIdAsync(id);
            if (img == null) return null;
            return new CostumeImageDTO
            {
                ImageId = img.ImageId,
                CostumeId = img.CostumeId,
                ImageUrl = img.ImageUrl
            };
        }

        public async Task<CostumeImageDTO> AddAsync(CostumeImageDTO dto)
        {
            var image = new CostumeImage
            {
                CostumeId = dto.CostumeId,
                ImageUrl = dto.ImageUrl,
                IsMain = dto.IsMain, // ➕ Thêm dòng này
                CreatedAt = DateTime.Now
            };
            var saved = await _repository.AddAsync(image);
            dto.ImageId = saved.ImageId;
            return dto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
