using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Repositories.DTOs;
using Services.Interfaces;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;
using Services.Implementations;

namespace SoulFashion.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly IBankTransferService _bankTransferService;
        private readonly ILogger<PaymentsController> _logger; // Added for logging
        private readonly IOrderService _orderService; // Added for order service
        public PaymentsController(
            IPaymentService paymentService,
            IVnPayService vnPayService,
            IMomoService momoService,
            IBankTransferService bankTransferService,
            ILogger<PaymentsController> logger,
            IOrderService orderService) // Added logger to constructor
        {
            _paymentService = paymentService;
            _vnPayService = vnPayService;
            _momoService = momoService;
            _bankTransferService = bankTransferService;
            _logger = logger; // Initialize logger
            _orderService = orderService;

        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId) =>
            Ok(await _paymentService.GetByOrderIdAsync(orderId));

        [HttpPost("vnpay")]
        public async Task<IActionResult> CreateVnPayLink([FromBody] PaymentDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var txnRef = "TXN" + dto.OrderId + DateTime.UtcNow.Ticks;

            await _paymentService.CreatePaymentAsync(dto, txnRef);
            var url = _vnPayService.CreatePaymentUrl(dto, ip, txnRef);
            return Ok(new { paymentUrl = url });
        }

        [HttpPost("momo")]
        public async Task<IActionResult> CreateMomoLink([FromBody] PaymentDto dto)
        {
            var txnRef = "MOMO" + dto.OrderId + DateTime.UtcNow.Ticks;
            await _paymentService.CreatePaymentAsync(dto, txnRef);
            var payUrl = await _momoService.CreatePaymentAsync(dto, txnRef);
            return Ok(new { paymentUrl = payUrl });
        }

        // ====== VNPay CALLBACK (GET) — VNPay redirect về bằng GET ======
        [HttpGet("vnpay-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayCallback()
        {
            var isValid = _vnPayService.ValidateResponse(Request.Query, out var txnRef);
            var code = Request.Query["vnp_ResponseCode"].ToString();
            var status = Request.Query["vnp_TransactionStatus"].ToString();

            if (isValid && code == "00" && status == "00")
            {
                await _paymentService.MarkAsPaid(txnRef);

                var payment = await _paymentService.GetByTxnRefAsync(txnRef); // lấy orderId
                return Redirect($"https://soul-of-fashion.vercel.app/payment-success?txnRef={txnRef}&orderId={payment.OrderId}");
            }

            return Redirect("https://soul-of-fashion.vercel.app/payment-failed");
        }



        // (tuỳ VNPay/hoặc muốn bắt notify dạng POST)
        [HttpPost("vnpay-callback")]
        [AllowAnonymous]
        public Task<IActionResult> VnPayCallbackPost() => VnPayCallback();

        [HttpPost("vnpay-callback-test")]
        public async Task<IActionResult> TestCallback([FromBody] string txnRef)
        {
            if (string.IsNullOrWhiteSpace(txnRef))
                return BadRequest(new { success = false, message = "Thiếu transactionRef" });

            await _paymentService.MarkAsPaid(txnRef);

            return Ok(new
            {
                success = true,
                message = "Thanh toán test thành công",
                transactionRef = txnRef
            });
        }

        [HttpPost("momo-notify")]
        public async Task<IActionResult> MomoNotify([FromBody] JsonElement body)
        {
            try
            {
                var orderId = body.GetProperty("orderId").GetString();
                var resultCode = body.GetProperty("resultCode").GetInt32();

                if (resultCode == 0 && !string.IsNullOrEmpty(orderId))
                {
                    await _paymentService.MarkAsPaid(orderId);
                    return Ok(new { message = "OK", status = "paid", transactionRef = orderId });
                }

                return BadRequest(new { message = "FAILED", status = "error" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Notify error", error = ex.Message });
            }
        }

        [HttpGet("momo-return")]
        public IActionResult MomoReturn([FromQuery] string orderId, [FromQuery] int resultCode)
        {
            if (resultCode == 0)
                return Ok(new { success = true, message = "Thanh toán thành công", transactionRef = orderId });

            return BadRequest(new { success = false, message = "Thanh toán thất bại hoặc bị hủy" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _paymentService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] PaymentDto dto)
        {
            await _paymentService.UpdateAsync(dto);
            return Ok(new { message = "Cập nhật thành công" });
        }
        [HttpGet("bank-transfer/{orderId}")]
        public async Task<IActionResult> GetBankTransferInfo(int orderId)
        {
            try
            {
                _logger.LogInformation("Getting bank transfer info for Order #{OrderId}", orderId);

                // ✅ Lấy order để biết TotalPrice
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Order not found"
                    });
                }

                // ✅ Lấy bank info từ service (đọc từ appsettings)
                var bankTransferInfo = await _bankTransferService.GetBankTransferInfoAsync(orderId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        bankTransferInfo.BankName,
                        bankTransferInfo.AccountNumber,
                        bankTransferInfo.AccountName,
                        bankTransferInfo.Branch,
                        bankTransferInfo.QrCodeUrl,
                        transferContent = $"SOULFASHION{orderId}", // nội dung CK
                        amount = order.TotalPrice,                 // ✅ lấy từ OrderDetailDto.TotalPrice
                        orderId = order.OrderId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get bank transfer info for Order #{OrderId}", orderId);
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpPost("bank-transfer/verify")]
        public async Task<IActionResult> VerifyBankTransfer([FromBody] BankTransferVerificationDto dto)
        {
            try
            {
                _logger.LogInformation("Verifying bank transfer for Order #{OrderId}", dto.OrderId);
                var isVerified = await _bankTransferService.VerifyBankTransferAsync(
                    dto.OrderId, 
                    dto.TransactionId, 
                    dto.Amount, 
                    dto.TransferDate);

                if (isVerified)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Bank transfer verified successfully"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bank transfer verification failed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify bank transfer for Order #{OrderId}", dto.OrderId);
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("bank-transfer/status/{orderId}")]
        public async Task<IActionResult> GetBankTransferStatus(int orderId)
        {
            try
            {
                _logger.LogInformation("Getting bank transfer status for Order #{OrderId}", orderId);
                var status = await _bankTransferService.GetTransferStatusAsync(orderId);
                return Ok(new
                {
                    success = true,
                    data = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get bank transfer status for Order #{OrderId}", orderId);
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("bank-transfer/pending")]
        public async Task<IActionResult> GetPendingTransfers()
        {
            try
            {
                _logger.LogInformation("Getting pending bank transfers");

                // Lấy tất cả payment đang pending
                var transfers = await _paymentService.GetPendingTransfersAsync();

                // Map thêm thông tin BankTransfer (tái sử dụng logic GetBankTransferInfo)
                var result = new List<object>();

                foreach (var t in transfers)
                {
                    var bankTransferInfo = await GetBankTransferInfo(t.OrderId); // <-- dùng lại hàm có sẵn
                    if (bankTransferInfo is OkObjectResult okResult)
                    {
                        var data = okResult.Value; // lấy data từ response
                        result.Add(data);
                    }
                }

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending bank transfers");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


    }
}
