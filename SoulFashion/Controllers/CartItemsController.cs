using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace SoulFashion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartItemsController : ControllerBase
    {
        private readonly ICartItemService _service;

        public CartItemsController(ICartItemService service)
        {
            _service = service;
        }

        // ✅ Lấy toàn bộ CartItems (chỉ dùng cho admin/debug)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var items = await _service.GetAllCartItemsAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy toàn bộ CartItems: {ex.Message}");
            }
        }

        // ✅ Lấy cart theo userId
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            try
            {
                var items = await _service.GetCartItemsByUserAsync(userId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy CartItems của user {userId}: {ex.Message}");
            }
        }

        // ✅ Thêm/cập nhật cart item
        [HttpPost("user/{userId}")]
        public async Task<IActionResult> AddOrUpdate(int userId, [FromQuery] int costumeId, [FromQuery] int quantity)
        {
            try
            {
                var item = await _service.AddOrUpdateCartItemAsync(userId, costumeId, quantity);
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi thêm/cập nhật CartItem: {ex.Message}");
            }
        }

        // ✅ Xoá item khỏi cart
        [HttpDelete("{cartItemId}")]
        public async Task<IActionResult> Delete(int cartItemId)
        {
            try
            {
                await _service.DeleteCartItemAsync(cartItemId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xoá CartItem: {ex.Message}");
            }
        }
    }
}
