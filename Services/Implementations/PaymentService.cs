using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
            // 🔍 Lấy tổng tiền từ đơn hàng
            var order = await _orderRepo.GetByIdAsync(dto.OrderId);
            if (order == null)
                throw new Exception("Order not found");

            var payment = new Payment
            {
                OrderId = dto.OrderId,
                Amount = order.TotalPrice ?? throw new Exception("Order.TotalPrice is null"),
                PaymentMethod = dto.PaymentMethod?.ToLower(),
                PaymentStatus = dto.PaymentStatus?.ToLower(),
                TransactionCode = txnRef,
                PaidAt = dto.PaidAt,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            return await _repo.CreateAsync(payment);
        }

        public async Task DeleteAsync(int paymentId)
        {
            await _repo.DeleteAsync(paymentId);
        }

        public async Task UpdateAsync(PaymentDto dto)
        {
            var payment = await _repo.GetByIdAsync(dto.PaymentId);
            if (payment == null)
                throw new Exception("Payment not found");

            payment.PaymentMethod = dto.PaymentMethod;
            payment.PaymentStatus = dto.PaymentStatus;
            payment.PaidAt = dto.PaidAt;
            payment.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(payment);
        }

        public async Task MarkAsPaid(string txnRef)
        {
            if (string.IsNullOrWhiteSpace(txnRef))
                throw new ArgumentException("Transaction reference cannot be empty");

            var payment = await _repo.GetByTxnRefAsync(txnRef);
            if (payment == null)
                throw new Exception($"Payment not found for transactionRef: {txnRef}");

            if (payment.PaymentStatus == "paid")
                return; // Đã thanh toán rồi thì bỏ qua

            // 🔹 Cập nhật Payment
            payment.PaymentStatus = "paid";
            payment.PaidAt = DateTime.Now;
            payment.UpdatedAt = DateTime.Now;

            // 🔹 Lấy Order kèm Deposit
            var order = await _orderRepo.GetByIdAsync(payment.OrderId);
            if (order == null)
                throw new Exception($"Order not found for payment: {payment.PaymentId}");

            order.Status = "confirmed";
            order.IsPaid = true;
            order.UpdatedAt = DateTime.Now;

            if (order.Deposit != null)
            {
                order.Deposit.DepositStatus = "paid";
                order.Deposit.PaymentMethod = payment.PaymentMethod ?? order.Deposit.PaymentMethod;
                order.Deposit.UpdatedAt = DateTime.Now;
            }

            // 🔹 Thêm status history
            order.StatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                Status = "confirmed",
                Note = "Thanh toán thành công qua " + (payment.PaymentMethod ?? "unknown"),
                ChangedAt = DateTime.Now
            });

            // 🔹 Lưu tất cả thay đổi
            await _repo.UpdateAsync(payment);
            await _orderRepo.UpdateAsync(order);
        }

    }
}
