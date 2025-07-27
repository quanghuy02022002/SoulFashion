// Services/Implementations/OrderStatusHistoryService.cs
using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;

namespace Services.Implementations
{
    public class OrderStatusHistoryService : IOrderStatusHistoryService
    {
        private readonly IOrderStatusHistoryRepository _repository;

        public OrderStatusHistoryService(IOrderStatusHistoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<OrderStatusHistory>> GetAllAsync()
            => await _repository.GetAllAsync();

        public async Task<OrderStatusHistory?> GetByIdAsync(int id)
            => await _repository.GetByIdAsync(id);

        public async Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId)
            => await _repository.GetByOrderIdAsync(orderId);

        public async Task<OrderStatusHistory> CreateAsync(OrderStatusHistoryDto dto)
        {
            var history = new OrderStatusHistory
            {
                OrderId = dto.OrderId,
                Status = dto.Status,
                Note = dto.Note,
                ChangedAt = DateTime.Now
            };
            return await _repository.CreateAsync(history);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
