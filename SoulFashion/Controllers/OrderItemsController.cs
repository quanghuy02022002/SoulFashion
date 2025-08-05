using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;
using System;
using System.Collections.Generic;
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

        // ✅ GET ALL
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
                return StatusCode(500, $"Lỗi server khi lấy tất cả OrderItems: {ex.Message}");
            }
        }

        // ✅ GET BY ID
        [HttpGet("{itemId}")]
        public async Task<IActionResult> GetById(int itemId)
        {
            try
            {
                var item = await _service.GetOrderItemByIdAsync(itemId);
                if (item == null) return NotFound("❌ OrderItem không tồn tại");
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server khi lấy OrderItem theo ID: {ex.Message}");
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

        // ✅ UPDATE OrderItem
        [HttpPut("{itemId}")]
        public async Task<IActionResult> Update(int itemId, [FromBody] OrderItemDto dto)
        {
            try
            {
                await _service.UpdateOrderItemAsync(itemId, dto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server khi cập nhật OrderItem: {ex.Message}");
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
