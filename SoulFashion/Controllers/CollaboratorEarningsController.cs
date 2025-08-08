using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollaboratorEarningsController : ControllerBase
{
    private readonly ICollaboratorEarningCrudService _crud;
    private readonly IEarningService _earningService; // rebuild per order

    public CollaboratorEarningsController(ICollaboratorEarningCrudService crud, IEarningService earningService)
    {
        _crud = crud;
        _earningService = earningService;
    }

    // GET: /api/collaboratorEarnings?userId=&orderId=&status=pending&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<CollaboratorEarningResponseDto>>> GetAll(
        [FromQuery] int? userId, [FromQuery] int? orderId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _crud.GetAllAsync(userId, orderId, status, page, pageSize);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: /api/collaboratorEarnings/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CollaboratorEarningResponseDto>> GetById([FromRoute] int id)
    {
        var e = await _crud.GetByIdAsync(id);
        if (e == null) return NotFound();
        return Ok(e);
    }

    // POST: /api/collaboratorEarnings
    [HttpPost]
    public async Task<ActionResult<CollaboratorEarningResponseDto>> Create([FromBody] CollaboratorEarningCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var created = await _crud.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.EarningId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: /api/collaboratorEarnings/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CollaboratorEarningResponseDto>> Update([FromRoute] int id, [FromBody] CollaboratorEarningUpdateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var updated = await _crud.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PATCH: /api/collaboratorEarnings/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<CollaboratorEarningResponseDto>> PatchStatus([FromRoute] int id, [FromBody] CollaboratorEarningStatusPatchDto dto)
    {
        try
        {
            var updated = await _crud.PatchStatusAsync(id, dto.Status);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: /api/collaboratorEarnings/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var ok = await _crud.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }

    // POST: /api/collaboratorEarnings/rebuild?orderId=123
    [HttpPost("rebuild")]
    public async Task<IActionResult> Rebuild([FromQuery] int orderId)
    {
        await _earningService.RebuildEarningsForOrderAsync(orderId);
        return Ok(new { message = "Rebuilt earnings for order", orderId });
    }
}