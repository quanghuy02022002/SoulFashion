using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;

public class CostumeService : ICostumeService
{
    private readonly ICostumeRepository _repository;
    private readonly IS3Service _s3Service; // ➕ thêm

    public CostumeService(ICostumeRepository repository, IS3Service s3Service)
    {
        _repository = repository;
        _s3Service = s3Service;

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
            CreatedByUserId = c.CreatedByUserId,
            CreatedByName = c.CreatedBy?.FullName, // ✅ map tên người tạo
            PriceSale = c.PriceSale,
            PriceRent = c.PriceRent,
            Quantity = c.Quantity,
            Size = c.Size,
            Condition = c.Condition,
            Gender = c.Gender,
            IsActive = c.IsActive,

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
            CreatedByUserId = c.CreatedByUserId,
            CreatedByName = c.CreatedBy?.FullName, // ✅ map người tạo
            PriceSale = c.PriceSale,
            PriceRent = c.PriceRent,
            Quantity = c.Quantity,
            Size = c.Size,
            Condition = c.Condition,
            Gender = c.Gender,
            IsActive = c.IsActive,
            Images = c.CostumeImages?.Select(i => new CostumeImageDTO
            {
                ImageId = i.ImageId,
                CostumeId = i.CostumeId,
                ImageUrl = i.ImageUrl,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList() ?? new List<CostumeImageDTO>()
        };
    }

    public async Task<CostumeDTO> AddAsync(CostumeDTO dto)
    {
        var c = new Costume
        {
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            CreatedByUserId = dto.CreatedByUserId,
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
    public async Task<List<CostumeDTO>> GetByUserIdAsync(int userId)
    {
        var costumes = await _repository.GetByUserIdAsync(userId);

        return costumes.Select(c => new CostumeDTO
        {
            CostumeId = c.CostumeId,
            Name = c.Name,
            Description = c.Description,
            CategoryId = c.CategoryId,
            CreatedByUserId = c.CreatedByUserId,
            CreatedByName = c.CreatedBy?.FullName,
            PriceSale = c.PriceSale,
            PriceRent = c.PriceRent,
            Quantity = c.Quantity,
            Size = c.Size,
            Condition = c.Condition,
            Gender = c.Gender,
            IsActive = c.IsActive,
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

    public async Task<bool> DeleteAsync(int id)
    {
        var costume = await _repository.GetByIdAsync(id); // đã Include(CostumeImages)
        if (costume == null) return false;

        // Xóa file S3 trước (best-effort)
        foreach (var img in costume.CostumeImages ?? Enumerable.Empty<CostumeImage>())
        {
            var key = TryExtractS3Key(img.ImageUrl);
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(key);
                }
                catch
                {
                    // TODO: log lỗi nếu cần (không throw để không chặn việc xóa DB)
                }
            }
        }

        // Xóa DB (Costume + CostumeImages nhờ cascade)
        return await _repository.DeleteAsync(id);
    }

    private static string? TryExtractS3Key(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;

        try
        {
            var uri = new Uri(imageUrl);

            // Virtual-hosted-style: https://bucket.s3.region.amazonaws.com/folder/file.jpg
            // -> key = uri.AbsolutePath.TrimStart('/')
            // Path-style (cũ): https://s3.region.amazonaws.com/bucket/folder/file.jpg
            // -> key = remove segment bucket ở đầu

            var path = uri.AbsolutePath.TrimStart('/');
            var host = uri.Host.ToLowerInvariant();

            if (host.StartsWith("s3.") || host.StartsWith("s3-") || host.Equals("s3.amazonaws.com"))
            {
                // path-style => segment[0] là bucket
                var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 2)
                    return string.Join('/', segs.Skip(1));
                return null;
            }
            else
            {
                // virtual-hosted-style => toàn bộ path là key
                return path;
            }
        }
        catch
        {
            // Fallback thô: lấy phần sau dấu '/' cuối cùng
            var idx = imageUrl!.LastIndexOf('/');
            return idx >= 0 && idx < imageUrl.Length - 1 ? imageUrl[(idx + 1)..] : imageUrl;
        }
    }

}
