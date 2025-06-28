using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            await _context.Payments.Where(p => p.OrderId == orderId).ToListAsync();

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment?> GetByTxnRefAsync(string txnRef)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.TransactionCode == txnRef);
        }

        public async Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }
    }

}
