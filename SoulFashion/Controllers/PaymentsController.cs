using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Repositories.DTOs;
using Services.Interfaces;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging; // Added for logging
using Microsoft.Extensions.DependencyInjection; // Added for GetRequiredService

namespace SoulFashion.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly IPayOsService _payOsService; // ✅ thêm
        private readonly ILogger<PaymentsController> _logger; // Added for logging

        public PaymentsController(
            IPaymentService paymentService,
            IVnPayService vnPayService,
            IMomoService momoService,
            IPayOsService payOsService,
            ILogger<PaymentsController> logger) // Added logger to constructor
        {
            _paymentService = paymentService;
            _vnPayService = vnPayService;
            _momoService = momoService;
            _payOsService = payOsService;
            _logger = logger; // Initialize logger
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
        // Tạo link thanh toán PayOS
        [HttpPost("payos")]
        public async Task<IActionResult> CreatePayOsLink([FromBody] PaymentDto dto)
        {
            try
            {
                _logger.LogInformation("Creating PayOS payment link for Order #{OrderId}", dto.OrderId);

                // Tạm thời sử dụng VnPay thay vì PayOS để test
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var txnRef = $"ORDER_{dto.OrderId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                
                var vnPayService = HttpContext.RequestServices.GetRequiredService<IVnPayService>();
                var paymentUrl = vnPayService.CreatePaymentUrl(dto, ipAddress, txnRef);

                _logger.LogInformation("VnPay payment URL created successfully for Order #{OrderId}", dto.OrderId);

                return Ok(new
                {
                    success = true,
                    message = "Payment link created successfully (using VnPay)",
                    orderId = dto.OrderId,
                    paymentUrl = paymentUrl,
                    qrCode = (string?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create VnPay payment link for Order #{OrderId}", dto.OrderId);
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to create payment link",
                    error = ex.Message,
                    orderId = dto.OrderId
                });
            }
        }

        // Webhook PayOS
        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                var signature = Request.Headers["x-signature"].ToString();

                Console.WriteLine($"PayOS Webhook: Received webhook from PayOS");
                Console.WriteLine($"PayOS Webhook: Body length: {body?.Length ?? 0}");
                Console.WriteLine($"PayOS Webhook: Signature header: {signature}");

                if (!_payOsService.VerifyWebhook(body, signature))
                {
                    Console.WriteLine("PayOS Webhook: Invalid signature, rejecting webhook");
                    return Unauthorized(new { 
                        success = false,
                        message = "Invalid signature",
                        timestamp = DateTime.UtcNow
                    });
                }

                using var doc = JsonDocument.Parse(body);
                var data = doc.RootElement.GetProperty("data");
                var status = data.GetProperty("status").GetString();
                var orderId = data.GetProperty("orderId").GetInt32();

                Console.WriteLine($"PayOS Webhook: Valid webhook for Order #{orderId}, Status: {status}");

                if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase))
                {
                    await _paymentService.MarkAsPaid(orderId.ToString());
                    Console.WriteLine($"PayOS Webhook: Order #{orderId} marked as paid successfully");
                }

                return Ok(new { 
                    success = true,
                    message = "Webhook processed successfully",
                    orderId,
                    status,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS Webhook Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                return StatusCode(500, new { 
                    success = false,
                    message = "Webhook processing failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // Return URL
        [HttpGet("payos-return")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsReturn([FromQuery] int orderId, [FromQuery] string status)
        {
            try
            {
                Console.WriteLine($"PayOS Return: Order #{orderId}, Status: {status}");

                if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase))
                {
                    await _paymentService.MarkAsPaid(orderId.ToString());
                    Console.WriteLine($"PayOS Return: Order #{orderId} marked as paid, redirecting to success page");
                    return Redirect($"https://soul-of-fashion.vercel.app/payment-success?orderId={orderId}");
                }

                Console.WriteLine($"PayOS Return: Order #{orderId} not paid, redirecting to failed page");
                return Redirect("https://soul-of-fashion.vercel.app/payment-failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS Return Error for Order #{orderId}: {ex.Message}");
                return Redirect("https://soul-of-fashion.vercel.app/payment-failed");
            }
        }

        // Cancel URL
        [HttpGet("payos-cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsCancel([FromQuery] int orderId, [FromQuery] string status)
        {
            try
            {
                Console.WriteLine($"PayOS Cancel: Order #{orderId}, Status: {status}");
                Console.WriteLine($"PayOS Cancel: Payment cancelled by user, redirecting to failed page");
                return Redirect("https://soul-of-fashion.vercel.app/payment-failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS Cancel Error for Order #{orderId}: {ex.Message}");
                return Redirect("https://soul-of-fashion.vercel.app/payment-failed");
            }
        }
    }
}
