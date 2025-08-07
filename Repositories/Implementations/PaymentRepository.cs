using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDBContext _context;

        public PaymentRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId) =>
            await _context.Payments
                          .Where(p => p.OrderId == orderId)
                          .ToListAsync();

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment?> GetByTxnRefAsync(string txnRef)
        {
            return await _context.Payments
                                 .FirstOrDefaultAsync(p => p.TransactionCode == txnRef);
        }

        public async Task<Payment> GetByIdAsync(int paymentId)
        {
            return await _context.Payments.FindAsync(paymentId);
        }

        public async Task DeleteAsync(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(Payment payment)
        {
            _context.Attach(payment);
            _context.Entry(payment).Property(p => p.PaymentStatus).IsModified = true;
            _context.Entry(payment).Property(p => p.PaidAt).IsModified = true;
            _context.Entry(payment).Property(p => p.UpdatedAt).IsModified = true;
            _context.Entry(payment).Property(p => p.PaymentMethod).IsModified = true;

            await _context.SaveChangesAsync();
        }
        public async Task<Payment?> GetPaymentWithOrderAsync(string txnRef)
        {
            return await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Deposit)
                .Include(p => p.Order)
                    .ThenInclude(o => o.StatusHistories)
                .FirstOrDefaultAsync(p => p.TransactionCode == txnRef);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}
