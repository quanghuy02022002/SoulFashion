using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class BankTransferRepository : IBankTransferRepository
    {
        private readonly AppDBContext _context;

        public BankTransferRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<BankTransfer?> GetByOrderIdAsync(int orderId)
        {
            return await _context.BankTransfers
                .Include(bt => bt.Order)
                .FirstOrDefaultAsync(bt => bt.OrderId == orderId);
        }

        public async Task<BankTransfer?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.BankTransfers
                .Include(bt => bt.Order)
                .FirstOrDefaultAsync(bt => bt.TransactionId == transactionId);
        }

        public async Task<BankTransfer> CreateAsync(BankTransfer bankTransfer)
        {
            _context.BankTransfers.Add(bankTransfer);
            await _context.SaveChangesAsync();
            return bankTransfer;
        }

        public async Task<BankTransfer> UpdateAsync(BankTransfer bankTransfer)
        {
            var existing = await _context.BankTransfers.FindAsync(bankTransfer.Id);
            if (existing == null)
                throw new Exception("BankTransfer not found");

            existing.TransactionId = bankTransfer.TransactionId;
            existing.Amount = bankTransfer.Amount;
            existing.Status = bankTransfer.Status;
            existing.TransferDate = bankTransfer.TransferDate;
            existing.Note = bankTransfer.Note; // ✅ nhớ gán Note
            existing.VerifiedBy = bankTransfer.VerifiedBy;
            existing.VerifiedAt = bankTransfer.VerifiedAt;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }


        public async Task<IEnumerable<BankTransfer>> GetPendingTransfersAsync()
        {
            return await _context.BankTransfers
                .Include(bt => bt.Order)
                .Where(bt => bt.Status == "Pending")
                .OrderByDescending(bt => bt.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<BankTransfer>> GetByStatusAsync(string status)
        {
            return await _context.BankTransfers
                .Include(bt => bt.Order)
                .Where(bt => bt.Status == status)
                .OrderByDescending(bt => bt.CreatedAt)
                .ToListAsync();
        }
    }
}

