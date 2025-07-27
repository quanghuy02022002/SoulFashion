// Services/Implementations/ReturnInspectionService.cs
using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ReturnInspectionService : IReturnInspectionService
    {
        private readonly IReturnInspectionRepository _repository;

        public ReturnInspectionService(IReturnInspectionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ReturnInspection>> GetAllAsync()
            => await _repository.GetAllAsync();

        public async Task<ReturnInspection?> GetByIdAsync(int id)
            => await _repository.GetByIdAsync(id);

        public async Task<ReturnInspection?> GetByOrderIdAsync(int orderId)
            => await _repository.GetByOrderIdAsync(orderId);

        public async Task<ReturnInspection> CreateAsync(ReturnInspectionDto dto)
        {
            var inspection = new ReturnInspection
            {
                OrderId = dto.OrderId,
                Condition = dto.Condition,
                PenaltyAmount = dto.PenaltyAmount,
                Note = dto.Note,
                CheckedAt = DateTime.Now
            };
            return await _repository.CreateAsync(inspection);
        }

        public async Task UpdateAsync(int id, ReturnInspectionDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) throw new Exception("ReturnInspection not found");

            existing.Condition = dto.Condition;
            existing.PenaltyAmount = dto.PenaltyAmount;
            existing.Note = dto.Note;
            existing.CheckedAt = DateTime.Now;

            await _repository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
