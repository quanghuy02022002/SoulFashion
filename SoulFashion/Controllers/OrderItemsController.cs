using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;
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
            var items = await _service.GetAllOrderItemsAsync();
            return Ok(items);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId) =>
            Ok(await _service.GetItemsByOrderIdAsync(orderId));

        [HttpPost("order/{orderId}")]
        public async Task<IActionResult> Create(int orderId, [FromBody] OrderItemDto dto)
        {
            var created = await _service.CreateOrderItemAsync(orderId, dto);
            return Ok(created);
        }

        [HttpDelete("{itemId}")]
        public async Task<IActionResult> Delete(int itemId)
        {
            await _service.DeleteOrderItemAsync(itemId);
            return NoContent();
        }
    }
}