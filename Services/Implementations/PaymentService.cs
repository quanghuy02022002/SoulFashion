using Repositories.DTOs;
using Repositories.Implementations;
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
        private readonly IOrderRepository _orderRepo; // nếu cần
        private readonly IEarningService _earningService;       // ✅ thêm
        private readonly ICollaboratorEearningRepository _earningRepo; // ✅ thêm
        private readonly AppDBContext _db;                       // ✅ thêm

        public PaymentService(
            IPaymentRepository repo,
            IOrderRepository orderRepo,
            IEarningService earningService,
            ICollaboratorEearningRepository earningRepo,
            AppDBContext db)
        {
            _repo = repo;
            _orderRepo = orderRepo;
            _earningService = earningService;
            _earningRepo = earningRepo;
            _db = db;
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

        /// <summary>
        /// Đánh dấu thanh toán đã hoàn tất và cập nhật luôn Order + Deposit
        /// </summary>
        public async Task MarkAsPaid(string txnRef)
        {
            if (string.IsNullOrWhiteSpace(txnRef))
                throw new ArgumentException("Transaction reference cannot be empty");

            var payment = await _repo.GetPaymentWithOrderAsync(txnRef)
                          ?? throw new Exception($"Payment not found for transactionRef: {txnRef}");

            if (string.Equals(payment.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                return; // đã thanh toán thì bỏ qua

            // 🔒 BẮT ĐẦU TRANSACTION TỪ ĐẦU
            await using var tx = await _db.Database.BeginTransactionAsync();

            // 1) Cập nhật Payment
            payment.PaymentStatus = "paid";
            payment.PaidAt = DateTime.Now;
            payment.UpdatedAt = DateTime.Now;

            // 2) Cập nhật Order + Deposit + History
            if (payment.Order != null)
            {
                payment.Order.Status = "confirmed";
                payment.Order.IsPaid = true;
                payment.Order.UpdatedAt = DateTime.Now;

                if (payment.Order.Deposit != null)
                {
                    payment.Order.Deposit.DepositStatus = "paid";
                    payment.Order.Deposit.PaymentMethod = payment.PaymentMethod;
                    payment.Order.Deposit.UpdatedAt = DateTime.Now;
                }

                // đảm bảo list không null
                payment.Order.StatusHistories ??= new List<OrderStatusHistory>();
                payment.Order.StatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = payment.Order.OrderId,
                    Status = "confirmed",
                    Note = $"Thanh toán thành công qua {payment.PaymentMethod}",
                    ChangedAt = DateTime.Now
                });
            }

            // 3) Lưu Payment + Order
            await _repo.UpdatePaymentWithOrderAsync(payment);

            // 4) Rebuild earnings 30/70 (idempotent)
            var orderId = payment.Order!.OrderId;
            await _earningService.RebuildEarningsForOrderAsync(orderId);

            // 5) Đổi status & paidAt cho earnings của order
            var now = DateTime.Now;
            var earnings = await _earningRepo.GetByOrderIdAsync(orderId);

            // --- A) Set PAID cho TẤT CẢ earnings (admin + collaborator) ---
            foreach (var e in earnings)
            {
                e.Status = "paid";
                e.PaidAt ??=  DateTime.Now; // chỉ set PaidAt nếu chưa có
                e.UpdatedAt = DateTime.Now;
            }

            // --- B) (Tuỳ chọn) Nếu CHỈ muốn set cho collaborator ---
            // var adminId = await _db.Users
            //     .Where(u => u.Role == "admin" && u.IsActive == true)
            //     .Select(u => u.UserId)
            //     .SingleAsync();
            // foreach (var e in earnings.Where(x => x.UserId != adminId))
            // {
            //     e.Status = "paid";
            //     e.PaidAt ??= now;
            //     e.UpdatedAt = now;
            // }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

    }
}
