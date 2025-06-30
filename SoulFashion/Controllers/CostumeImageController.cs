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
        var images = await _service.GetByCostumeIdAsync(costumeId);
        return Ok(images);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] CostumeImageUploadRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var baseName = string.IsNullOrWhiteSpace(request.FileName)
                ? Path.GetFileNameWithoutExtension(request.File.FileName)
                : request.FileName;

            var url = await _s3Service.UploadFileAsync(request.File, baseName);

            var dto = new CostumeImageDTO
            {
                CostumeId = request.CostumeId,
                ImageUrl = url
            };

            var result = await _service.AddAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Upload ERROR] {ex.Message}");
            return StatusCode(500, $"Upload failed: {ex.Message}");
        }
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var image = await _service.GetByIdAsync(id);
        if (image == null) return NotFound();

        var key = new Uri(image.ImageUrl).Segments.Last();
        await _s3Service.DeleteFileAsync(key);
        await _service.DeleteAsync(id);

        return NoContent();
    }
}
