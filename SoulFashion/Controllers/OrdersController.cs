using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;

namespace SoulFashion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var orders = await _service.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server khi lấy danh sách đơn hàng: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var order = await _service.GetOrderByIdAsync(id);
                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy đơn hàng: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(OrderDto dto)
        {
            try
            {
                var created = await _service.CreateOrderAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.OrderId }, new { created.OrderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi tạo đơn hàng: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, OrderDto dto)
        {
            try
            {
                await _service.UpdateOrderAsync(id, dto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi cập nhật đơn hàng: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteOrderAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xóa đơn hàng: {ex.Message}");
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, OrderStatusUpdateDto dto)
        {
            try
            {
                await _service.UpdateOrderStatusAsync(id, dto.Status);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi cập nhật trạng thái đơn hàng: {ex.Message}");
            }
        }
    }

}
