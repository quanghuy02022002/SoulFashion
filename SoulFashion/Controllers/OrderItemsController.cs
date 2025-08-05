using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace SoulFashion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderItemsController : ControllerBase
    {
        private readonly IOrderItemService _service;

        public OrderItemsController(IOrderItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var items = await _service.GetAllOrderItemsAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                // In lỗi ra console
                Console.WriteLine("‼ Lỗi khi lấy OrderItems: " + ex.ToString());

                // Trả ra message rõ ràng
                return StatusCode(500, $"‼ Lỗi server: {ex.Message}\n{ex.StackTrace}");
            }
        }


        // ✅ GET BY ORDER ID
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            try
            {
                var items = await _service.GetItemsByOrderIdAsync(orderId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server khi lấy OrderItems theo OrderId: {ex.Message}");
            }
        }

        // ✅ CREATE OrderItem
        [HttpPost("order/{orderId}")]
        public async Task<IActionResult> Create(int orderId, [FromBody] OrderItemDto dto)
        {
            try
            {
                var created = await _service.CreateOrderItemAsync(orderId, dto);
                return Ok(created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server khi tạo OrderItem: {ex.Message}");
            }
        }

        // ✅ DELETE OrderItem
        [HttpDelete("{itemId}")]
        public async Task<IActionResult> Delete(int itemId)
        {
            try
            {
                await _service.DeleteOrderItemAsync(itemId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server khi xoá OrderItem: {ex.Message}");
            }
        }
    }
}
