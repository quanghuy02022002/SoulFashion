using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class DepositService : IDepositService
    {
        private readonly IDepositRepository _repository;
        public DepositService(IDepositRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Deposit>> GetAllAsync()
            => await _repository.GetAllAsync();

        public async Task<Deposit?> GetByIdAsync(int id)
            => await _repository.GetByIdAsync(id);

        public async Task<Deposit> CreateAsync(DepositDto dto)
        {
            var deposit = new Deposit
            {
                OrderId = dto.OrderId,
                DepositAmount = dto.DepositAmount,
                PaymentMethod = dto.PaymentMethod,
                DepositStatus = dto.DepositStatus,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            return await _repository.CreateAsync(deposit);
        }

        public async Task UpdateAsync(int id, DepositDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) throw new Exception("Deposit not found");

            existing.DepositAmount = dto.DepositAmount;
            existing.PaymentMethod = dto.PaymentMethod;
            existing.DepositStatus = dto.DepositStatus;
            existing.UpdatedAt = DateTime.Now;

            await _repository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
