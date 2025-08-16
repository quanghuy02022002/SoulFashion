using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IBankTransferRepository
    {
        Task<BankTransfer?> GetByOrderIdAsync(int orderId);
        Task<BankTransfer?> GetByTransactionIdAsync(string transactionId);
        Task<BankTransfer> CreateAsync(BankTransfer bankTransfer);
        Task<BankTransfer> UpdateAsync(BankTransfer bankTransfer);
        Task<IEnumerable<BankTransfer>> GetPendingTransfersAsync();
        Task<IEnumerable<BankTransfer>> GetByStatusAsync(string status);
    }
}

