using Microsoft.AspNetCore.Http;
using Repositories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentDto dto, string ipAddress, string txnRef);
        bool ValidateResponse(IQueryCollection vnpParams, out string txnRef);
    }

}
