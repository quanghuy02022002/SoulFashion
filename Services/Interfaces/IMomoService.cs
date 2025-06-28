using Repositories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IMomoService
    {
        Task<string> CreatePaymentAsync(PaymentDto dto, string txnRef);
    }

}
