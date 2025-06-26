using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;

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

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId) =>
            Ok(await _service.GetItemsByOrderIdAsync(orderId));

        [HttpPost]
        public async Task<IActionResult> Create(OrderItemDto dto)
        {
            var created = await _service.CreateOrderItemAsync(dto);
            return Ok(created);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteOrderItemAsync(id);
            return NoContent();
        }
    }

}
