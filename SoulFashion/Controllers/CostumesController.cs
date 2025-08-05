using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;
using System.Security.Claims;

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
            dto.CreatedByUserId = int.Parse(User.FindFirst("id")!.Value); // tự động lấy người dùng hiện tại
            var result = await _service.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.CostumeId }, result);
        }




        [HttpPut("{id}")]
        [Authorize(Roles = "admin,Collaborator")]
        public async Task<IActionResult> Update(int id, [FromBody] CostumeDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue("id"));
            var role = User.FindFirstValue(ClaimTypes.Role);

            var costume = await _service.GetByIdAsync(id);
            if (costume == null) return NotFound();

            // 🔐 Chỉ admin hoặc người tạo mới được sửa
            if (role != "admin" && costume.CreatedByUserId != userId)
                return Forbid("Only the creator or admin can update this costume");

            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }
        [HttpGet("by-user")]
        [Authorize(Roles = "admin,Collaborator")]
        public async Task<IActionResult> GetByUser()
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            var result = await _service.GetByUserIdAsync(userId);
            return Ok(result);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,Collaborator")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue("id"));
            var role = User.FindFirstValue(ClaimTypes.Role);

            var costume = await _service.GetByIdAsync(id);
            if (costume == null) return NotFound();

            // 🔐 Chỉ admin hoặc người tạo mới được xoá
            if (role != "admin" && costume.CreatedByUserId != userId)
                return Forbid("Only the creator or admin can delete this costume");

            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
