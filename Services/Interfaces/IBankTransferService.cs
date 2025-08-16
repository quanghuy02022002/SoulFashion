
using Repositories.DTOs;
using System;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IBankTransferService
    {
        Task<BankTransferInfoDto> GetBankTransferInfoAsync(int orderId);
        Task<bool> VerifyBankTransferAsync(int orderId, string transactionId, decimal amount, DateTime transferDate);
        Task<BankTransferStatusDto> GetTransferStatusAsync(int orderId);
    }
}

