using Repositories.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment?> GetByTxnRefAsync(string txnRef);
        Task<Payment?> GetByIdAsync(int paymentId);
        Task<Payment?> GetPaymentWithOrderAsync(string txnRef);
        Task DeleteAsync(int paymentId);
        Task UpdateAsync(Payment payment);

        /// <summary>
        /// Cập nhật Payment kèm Order + Deposit + StatusHistories trong 1 transaction
        /// </summary>
        Task UpdatePaymentWithOrderAsync(Payment payment);

    }
}
