using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class CostumeImageRepository : ICostumeImageRepository
    {
        private readonly AppDBContext _context;

        public CostumeImageRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<List<CostumeImage>> GetByCostumeIdAsync(int costumeId)
        {
            return await _context.CostumeImages
                .Where(i => i.CostumeId == costumeId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<CostumeImage?> GetByIdAsync(int id)
        {
            return await _context.CostumeImages.FindAsync(id);
        }

        public async Task<CostumeImage> AddAsync(CostumeImage image)
        {
            _context.CostumeImages.Add(image);
            await _context.SaveChangesAsync();
            return image;
        }
        public async Task UnsetMainImageAsync(int costumeId)
        {
            var mainImages = _context.CostumeImages
                .Where(i => i.CostumeId == costumeId && i.IsMain);

            foreach (var img in mainImages)
            {
                img.IsMain = false;
                img.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var img = await _context.CostumeImages.FindAsync(id);
            if (img == null) return false;

            _context.CostumeImages.Remove(img);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
