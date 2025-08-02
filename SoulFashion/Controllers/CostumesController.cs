using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;

namespace SoulFashion.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CostumesController : ControllerBase
    {
        private readonly ICostumeService _service;

        public CostumesController(ICostumeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var data = await _service.GetAllAsync(search, page, pageSize);
            var total = await _service.CountAsync(search);
            return Ok(new
            {
                data,
                pagination = new { total, page, pageSize }
            });
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "admin,Collaborator")]
        public async Task<IActionResult> Create([FromBody] CostumeDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.CostumeId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,Collaborator")]
        public async Task<IActionResult> Update(int id, [FromBody] CostumeDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.UpdateAsync(id, dto);
                return Ok(result);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,Collaborator")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
