// Controllers/ReviewController.cs
using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Repositories.Requests;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _service;

    public ReviewController(IReviewService service)
    {
        _service = service;
    }

    /// GET /api/review/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    /// GET /api/review/costume/{costumeId}?page=1&pageSize=10
    [HttpGet("costume/{costumeId:int}")]
    public async Task<IActionResult> GetByCostume(int costumeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0) return BadRequest("page and pageSize must be positive.");
        var (items, total) = await _service.GetByCostumeAsync(costumeId, page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }

    /// POST /api/review
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReviewCreateRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var dto = new ReviewDTO
        {
            UserId = req.UserId,
            CostumeId = req.CostumeId,
            Rating = req.Rating,
            Comment = req.Comment
        };

        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ReviewId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message); // user đã review rồi
        }
    }

    /// PUT /api/review/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ReviewUpdateRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var dto = new ReviewDTO { Rating = req.Rating, Comment = req.Comment };
        var ok = await _service.UpdateAsync(id, dto);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// DELETE /api/review/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }
}
