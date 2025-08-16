using Microsoft.Extensions.Configuration;
using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class BankTransferService : IBankTransferService
    {
        private readonly IConfiguration _config;
        private readonly IBankTransferRepository _bankTransferRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly ILogger<BankTransferService> _logger;

        private readonly string _bankName;
        private readonly string _accountNumber;
        private readonly string _accountName;
        private readonly string _branch;
        private readonly string _transferContent;

        public BankTransferService(
            IConfiguration config,
            IBankTransferRepository bankTransferRepo,
            IOrderRepository orderRepo,
            ILogger<BankTransferService> logger)
        {
            _config = config;
            _bankTransferRepo = bankTransferRepo;
            _orderRepo = orderRepo;
            _logger = logger;

            _bankName = config["BankTransfer:BankName"] ?? "Vietcombank";
            _accountNumber = config["BankTransfer:AccountNumber"] ?? "";
            _accountName = config["BankTransfer:AccountName"] ?? "";
            _branch = config["BankTransfer:Branch"] ?? "";
            _transferContent = config["BankTransfer:TransferContent"] ?? "SOULFASHION";
        }

        public async Task<BankTransferInfoDto> GetBankTransferInfoAsync(int orderId)
        {
            try
            {
                var order = await _orderRepo.GetByIdAsync(orderId);
                if (order == null)
                {
                    throw new ArgumentException($"Order with ID {orderId} not found");
                }

                if (!order.TotalPrice.HasValue)
                {
                    throw new InvalidOperationException($"Order {orderId} has no total price");
                }

                // Tạo QR code URL cho Vietcombank
                var qrCodeUrl = GenerateVietcombankQrCode(orderId, order.TotalPrice.Value);

                return new BankTransferInfoDto
                {
                    BankName = _bankName,
                    AccountNumber = _accountNumber,
                    AccountName = _accountName,
                    Branch = _branch,
                    TransferContent = $"{_transferContent}{orderId}",
                    Amount = order.TotalPrice.Value,
                    OrderId = orderId,
                    QrCodeUrl = qrCodeUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bank transfer info for Order #{OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> VerifyBankTransferAsync(int orderId, string transactionId, decimal amount, DateTime transferDate)
        {
            try
            {
                var order = await _orderRepo.GetByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order #{OrderId} not found for bank transfer verification", orderId);
                    return false;
                }

                if (!order.TotalPrice.HasValue)
                {
                    _logger.LogWarning("Order #{OrderId} has no total price", orderId);
                    return false;
                }

                // Kiểm tra số tiền có khớp không
                if (Math.Abs(order.TotalPrice.Value - amount) > 0.01m)
                {
                    _logger.LogWarning("Amount mismatch for Order #{OrderId}: Expected {Expected}, Got {Actual}", 
                        orderId, order.TotalPrice.Value, amount);
                    return false;
                }

                // Kiểm tra xem transaction ID đã tồn tại chưa
                var existingTransfer = await _bankTransferRepo.GetByTransactionIdAsync(transactionId);
                if (existingTransfer != null)
                {
                    _logger.LogWarning("Transaction ID {TransactionId} already exists", transactionId);
                    return false;
                }

                // Tạo hoặc cập nhật bank transfer record
                var bankTransfer = await _bankTransferRepo.GetByOrderIdAsync(orderId);
                if (bankTransfer == null)
                {
                    bankTransfer = new BankTransfer
                    {
                        OrderId = orderId,
                        TransactionId = transactionId,
                        Amount = amount,
                        TransferDate = transferDate,
                        Status = "Completed",
                        VerifiedAt = DateTime.UtcNow
                    };
                    await _bankTransferRepo.CreateAsync(bankTransfer);
                }
                else
                {
                    bankTransfer.TransactionId = transactionId;
                    bankTransfer.Amount = amount;
                    bankTransfer.TransferDate = transferDate;
                    bankTransfer.Status = "Completed";
                    bankTransfer.VerifiedAt = DateTime.UtcNow;
                    await _bankTransferRepo.UpdateAsync(bankTransfer);
                }

                // Cập nhật trạng thái đơn hàng
                order.Status = "Paid";
                await _orderRepo.UpdateAsync(order);

                _logger.LogInformation("Bank transfer verified successfully for Order #{OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying bank transfer for Order #{OrderId}", orderId);
                return false;
            }
        }

        public async Task<BankTransferStatusDto> GetTransferStatusAsync(int orderId)
        {
            try
            {
                var bankTransfer = await _bankTransferRepo.GetByOrderIdAsync(orderId);
                if (bankTransfer == null)
                {
                    return new BankTransferStatusDto
                    {
                        OrderId = orderId,
                        Status = "Not Found"
                    };
                }

                return new BankTransferStatusDto
                {
                    OrderId = orderId,
                    Status = bankTransfer.Status,
                    TransactionId = bankTransfer.TransactionId,
                    TransferDate = bankTransfer.TransferDate,
                    Amount = bankTransfer.Amount,
                    Note = bankTransfer.Note
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transfer status for Order #{OrderId}", orderId);
                return new BankTransferStatusDto
                {
                    OrderId = orderId,
                    Status = "Error"
                };
            }
        }

        private string GenerateVietcombankQrCode(int orderId, decimal amount)
        {
            // Tạo QR code đơn giản với thông tin chuyển khoản
            var transferContent = $"{_transferContent}{orderId}";
            
            // Tạo dữ liệu QR đơn giản
            var qrData = $"Ngân hàng: {_bankName}\nSố TK: {_accountNumber}\nTên TK: {_accountName}\nSố tiền: {amount:N0} VND\nNội dung: {transferContent}";
            
            // Tạo URL QR code
            return $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(qrData)}&format=png&margin=10&ecc=M";
        }
    }
}

