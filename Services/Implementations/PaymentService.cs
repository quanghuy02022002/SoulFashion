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
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repo;
        private readonly IOrderRepository _orderRepo;

        public PaymentService(IPaymentRepository repo, IOrderRepository orderRepo)
        {
            _repo = repo;
            _orderRepo = orderRepo;
        }


        public async Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId) =>
            await _repo.GetByOrderIdAsync(orderId);

        public async Task<Payment> CreatePaymentAsync(PaymentDto dto, string txnRef)
        {
            var payment = new Payment
            {
                OrderId = dto.OrderId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod.ToLower(),
                PaymentStatus = dto.PaymentStatus.ToLower(),
                TransactionCode = txnRef,
                PaidAt = dto.PaidAt,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            return await _repo.CreateAsync(payment);
        }

        public async Task MarkAsPaid(string txnRef)
        {
            var payment = await _repo.GetByTxnRefAsync(txnRef);
            if (payment != null && payment.PaymentStatus != "paid")
            {
                payment.PaymentStatus = "paid";
                payment.PaidAt = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;
                await _repo.UpdateAsync(payment);

                // 👉 Đồng thời cập nhật trạng thái đơn hàng
                var order = await _orderRepo.GetByIdAsync(payment.OrderId);
                if (order != null && order.Status == "pending")
                {
                    order.Status = "confirmed";
                    order.UpdatedAt = DateTime.Now;
                    await _orderRepo.UpdateAsync(order);
                }
            }
        }

    }

}
