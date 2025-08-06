using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Repositories.Requests;
using Services.Interfaces;

[Route("api/[controller]")]
[ApiController]
public class CostumeImageController : ControllerBase
{
    private readonly ICostumeImageService _service;
    private readonly IS3Service _s3Service;

    public CostumeImageController(ICostumeImageService service, IS3Service s3Service)
    {
        _service = service;
        _s3Service = s3Service;
    }

    [HttpGet("costume/{costumeId}")]
    public async Task<IActionResult> GetByCostumeId(int costumeId)
    {
        try
        {
            var images = await _service.GetByCostumeIdAsync(costumeId);
            return Ok(images);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString()); // ← trả về lỗi chi tiết
        }
    }


    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] CostumeImageUploadRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var baseName = string.IsNullOrWhiteSpace(request.FileName)
            ? Path.GetFileNameWithoutExtension(request.File.FileName)
            : request.FileName;

        var url = await _s3Service.UploadFileAsync(request.File, baseName);

        var dto = new CostumeImageDTO
        {
            CostumeId = request.CostumeId,
            ImageUrl = url,
            IsMain = request.IsMain
        };

        try
        {
            var result = await _service.AddAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Upload failed: {ex.Message} - Inner: {ex.InnerException?.Message}");
        }
    }



    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var image = await _service.GetByIdAsync(id);
        if (image == null)
            return NotFound();

        // Kiểm tra nếu URL hợp lệ trước khi parse
        if (!string.IsNullOrEmpty(image.ImageUrl))
        {
            try
            {
                var key = new Uri(image.ImageUrl).Segments.Last();
                await _s3Service.DeleteFileAsync(key);
            }
            catch (Exception ex)
            {
                // Optional: log lỗi nếu parse URL thất bại
                return StatusCode(500, $"Failed to parse S3 URL: {ex.Message}");
            }
        }

        await _service.DeleteAsync(id);
        return NoContent();
    }

}
