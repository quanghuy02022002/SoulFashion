using Repositories.DTOs;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId);
        Task<Payment> CreatePaymentAsync(PaymentDto dto, string txnRef);
        Task DeleteAsync(int paymentId);
        Task UpdateAsync(PaymentDto dto);
        Task MarkAsPaid(string txnRef);
    }

}
