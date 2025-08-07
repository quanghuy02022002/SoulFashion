using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment?> GetByTxnRefAsync(string txnRef);
        Task DeleteAsync(int paymentId);
        Task<Payment> GetByIdAsync(int paymentId);

        Task UpdateAsync(Payment payment);
    }

}
